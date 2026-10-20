using System.Collections;
using UnityEngine;

/// <summary>
/// Pistola del jugador. Se agrega en tiempo de ejecucion desde <see cref="PistolPickup"/>.
/// Disparo por raycast con trazador visible, chispa de impacto y dano a todo lo que tenga <see cref="Health"/>.
/// Tiene cargador de 8 balas: al vaciarse hay que recargar con R usando la municion de reserva del
/// <see cref="Inventory"/> (se consigue en cajas de municion).
/// </summary>
public class PistolWeapon : MonoBehaviour
{
    public Transform muzzle;
    public Camera cam;
    public float damage = 25f;
    public float range = 100f;
    public float cooldown = 0.25f;
    public float recoil = 0.05f;
    public AudioClip fireSound;

    [Header("Municion")]
    /// <summary>Balas que caben en el cargador. La pistola empieza con el cargador lleno.</summary>
    public int magazineSize = 8;
    /// <summary>Segundos que tarda la recarga.</summary>
    public float reloadSeconds = 1.6f;
    public AudioClip reloadSound;

    /// <summary>Momento (Time.time) del ultimo disparo. El enemigo lo usa para oirlo.</summary>
    public static float LastShotTime = -100f;

    /// <summary>Balas que quedan en el cargador.</summary>
    public int AmmoInMagazine { get; private set; }

    /// <summary>Verdadero mientras se recarga.</summary>
    public bool IsReloading { get; private set; }

    float nextShot;
    float flashUntil;
    Vector3 restPos;
    Light flash;
    Material fxMat;
    PlayerController owner;
    Inventory inventory;

    void Start()
    {
        restPos = transform.localPosition;
        if (cam == null) cam = Camera.main;
        if (muzzle == null) muzzle = transform;

        owner = GetComponentInParent<PlayerController>();
        if (owner == null) owner = FindFirstObjectByType<PlayerController>();
        if (owner != null) inventory = Inventory.Of(owner);
        AmmoInMagazine = magazineSize;

        // Material simple siempre incluido en las builds
        fxMat = new Material(Shader.Find("Sprites/Default"));

        // Destello del canon
        var fgo = new GameObject("MuzzleFlash");
        fgo.transform.SetParent(muzzle, false);
        flash = fgo.AddComponent<Light>();
        flash.type = LightType.Point;
        flash.color = new Color(1f, 0.7f, 0.3f);
        flash.range = 6f;
        flash.intensity = 4f;
        flash.enabled = false;

        // Luz suave para que la pistola se vea aunque la escena este oscura
        var hl = new GameObject("HeldLight");
        hl.transform.SetParent(cam.transform, false);
        hl.transform.localPosition = new Vector3(0.1f, 0.15f, 0.1f);
        var l = hl.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.9f, 0.95f, 1f);
        l.range = 1.5f;
        l.intensity = 0.6f;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;       // en pausa o con el jugador muerto

        if (Input.GetKeyDown(KeyCode.R)) TryReload();

        if (Input.GetMouseButtonDown(0) && Time.time >= nextShot && !IsReloading)
        {
            if (AmmoInMagazine <= 0)
            {
                nextShot = Time.time + 0.4f;
                Say("Cargador vacío. Pulsa R para recargar.");
            }
            else
            {
                nextShot = Time.time + cooldown;
                Fire();
            }
        }

        if (flash.enabled && Time.time > flashUntil) flash.enabled = false;

        // vuelve a su sitio; al recargar baja un poco
        Vector3 target = IsReloading ? restPos + new Vector3(0f, -0.12f, -0.03f) : restPos;
        transform.localPosition = Vector3.Lerp(transform.localPosition, target, Time.deltaTime * 15f);
    }

    /// <summary>Intenta recargar con la municion de reserva.</summary>
    public void TryReload()
    {
        if (IsReloading || AmmoInMagazine >= magazineSize) return;

        int reserve = inventory != null ? inventory.reserveAmmo : 0;
        if (reserve <= 0)
        {
            Say("No tienes munición de reserva. Busca una caja.");
            return;
        }
        StartCoroutine(Reload());
    }

    IEnumerator Reload()
    {
        IsReloading = true;
        if (reloadSound != null) AudioSource.PlayClipAtPoint(reloadSound, transform.position);
        Say("Recargando…");

        yield return new WaitForSeconds(reloadSeconds);

        int take = Mathf.Min(magazineSize - AmmoInMagazine, inventory != null ? inventory.reserveAmmo : 0);
        AmmoInMagazine += take;
        if (inventory != null) inventory.reserveAmmo -= take;
        IsReloading = false;
    }

    void Say(string text)
    {
        if (owner != null) owner.ShowMessage(text, 2f);
    }

    void Fire()
    {
        AmmoInMagazine--;
        LastShotTime = Time.time;
        if (AmmoInMagazine == 0) Say("Último disparo. Pulsa R para recargar.");

        if (fireSound != null)
            AudioSource.PlayClipAtPoint(fireSound, transform.position);

        flash.enabled = true;
        flashUntil = Time.time + 0.05f;
        transform.localPosition = restPos - new Vector3(0f, 0f, recoil);

        Vector3 origin = cam.transform.position;
        Vector3 dir = cam.transform.forward;
        Vector3 end = origin + dir * range;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Ignore))
        {
            end = hit.point;

            var health = hit.collider.GetComponentInParent<Health>();
            if (health != null) health.TakeDamage(damage);

            SpawnImpact(hit.point, hit.normal);
        }

        SpawnTracer(muzzle.position, end);
    }

    void SpawnTracer(Vector3 a, Vector3 b)
    {
        var go = new GameObject("Tracer");
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.startWidth = 0.02f;
        lr.endWidth = 0.006f;
        lr.material = fxMat;
        lr.startColor = new Color(1f, 0.9f, 0.5f, 1f);
        lr.endColor = new Color(1f, 0.6f, 0.2f, 0.3f);
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        Destroy(go, 0.06f);
    }

    void SpawnImpact(Vector3 point, Vector3 normal)
    {
        var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(s.GetComponent<Collider>());
        s.transform.position = point + normal * 0.01f;
        s.transform.localScale = Vector3.one * 0.08f;

        var r = s.GetComponent<Renderer>();
        var m = new Material(fxMat);
        m.color = new Color(1f, 0.8f, 0.3f, 1f);
        r.material = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Destroy(s, 0.15f);
    }
}
