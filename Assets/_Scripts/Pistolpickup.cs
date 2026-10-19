using UnityEngine;

// Poner en el grupo "Pistola" (antes agrega un Box Collider al mismo objeto).
// Al recogerla crea una copia en la mano del jugador, la ajusta de tamano y posicion
// automaticamente y elimina la del suelo.
public class PistolPickup : Interactable
{
    [Header("Posicion en pantalla (centro de la pistola, relativa a la camara)")]
    public Vector3 heldPosition = new Vector3(0.16f, -0.12f, 0.5f);
    public Vector3 heldEuler = Vector3.zero;     // si apunta hacia atras prueba (0,180,0)
    public float heldLength = 0.22f;             // largo visible en metros (se autoajusta)

    [Header("Disparo")]
    public float damage = 25f;
    public float range = 100f;
    public AudioClip fireSound;

    void Reset() { prompt = "Pulsa E para recoger la pistola"; }

    public override void Interact(PlayerController player)
    {
        Transform cam = player.cam.transform;

        GameObject held = Instantiate(gameObject, cam);
        held.SetActive(true);

        foreach (var c in held.GetComponentsInChildren<Collider>()) c.enabled = false;
        var pickup = held.GetComponent<PistolPickup>();
        if (pickup != null) pickup.enabled = false;

        foreach (var r in held.GetComponentsInChildren<Renderer>())
        {
            r.enabled = true;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        Vector3 center = FitAndPlace(held.transform, cam);

        var muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(held.transform, true);
        muzzle.transform.position = center + cam.forward * (heldLength * 0.5f);

        var weapon = held.AddComponent<PistolWeapon>();
        weapon.muzzle = muzzle.transform;
        weapon.cam = player.cam;
        weapon.damage = damage;
        weapon.range = range;
        weapon.fireSound = fireSound;

        player.ShowMessage("Pistola obtenida. Clic izquierdo para disparar.", 4f);
        PlaySound();
        Destroy(gameObject);
    }

    // Escala la pistola a heldLength y centra su caja en heldPosition
    Vector3 FitAndPlace(Transform held, Transform cam)
    {
        held.localRotation = Quaternion.Euler(heldEuler);
        held.localScale = Vector3.one;
        held.localPosition = Vector3.zero;

        var rends = held.GetComponentsInChildren<Renderer>();
        Vector3 target = cam.TransformPoint(heldPosition);
        if (rends.Length == 0) { held.position = target; return target; }

        Bounds b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);

        float longest = Mathf.Max(b.size.x, b.size.y, b.size.z);
        if (longest > 0.0001f)
            held.localScale = Vector3.one * (heldLength / longest);

        b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);

        held.position += target - b.center;
        return target;
    }
}