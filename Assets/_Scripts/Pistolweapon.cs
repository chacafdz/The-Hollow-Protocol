using UnityEngine;

// Se agrega en tiempo de ejecucion desde PistolPickup.
// Disparo por raycast: trazador visible, chispa de impacto y dano a todo lo que tenga Health.
public class PistolWeapon : MonoBehaviour
{
    public Transform muzzle;
    public Camera cam;
    public float damage = 25f;
    public float range = 100f;
    public float cooldown = 0.25f;
    public float recoil = 0.05f;
    public AudioClip fireSound;

    float nextShot;
    float flashUntil;
    Vector3 restPos;
    Light flash;
    Material fxMat;

    void Start()
    {
        restPos = transform.localPosition;
        if (cam == null) cam = Camera.main;
        if (muzzle == null) muzzle = transform;

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
        if (Input.GetMouseButtonDown(0) && Time.time >= nextShot)
        {
            nextShot = Time.time + cooldown;
            Fire();
        }

        if (flash.enabled && Time.time > flashUntil) flash.enabled = false;
        transform.localPosition = Vector3.Lerp(transform.localPosition, restPos, Time.deltaTime * 15f);
    }

    void Fire()
    {
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