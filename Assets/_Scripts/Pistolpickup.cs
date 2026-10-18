using UnityEngine;

// Poner en el grupo "Pistola" (antes agrega un Box Collider al mismo objeto).
// Al recogerla, crea una copia en la mano del jugador y elimina la del suelo.
public class PistolPickup : Interactable
{
    [Header("Posicion en pantalla (relativa a la camara)")]
    public Vector3 heldPosition = new Vector3(0.22f, -0.22f, 0.45f);
    public Vector3 heldEuler = Vector3.zero;
    public float heldScale = 1f;

    [Header("Sonido")]
    public AudioClip fireSound;

    void Reset() { prompt = "Pulsa E para recoger la pistola"; }

    public override void Interact(PlayerController player)
    {
        Transform cam = player.cam.transform;

        GameObject held = Instantiate(gameObject, cam);

        // La copia no debe tener colisiones ni volver a ser recogible
        foreach (var c in held.GetComponentsInChildren<Collider>()) c.enabled = false;
        var pickup = held.GetComponent<PistolPickup>();
        if (pickup != null) pickup.enabled = false;

        held.transform.localPosition = heldPosition;
        held.transform.localRotation = Quaternion.Euler(heldEuler);
        held.transform.localScale = Vector3.one * heldScale;

        foreach (var r in held.GetComponentsInChildren<Renderer>())
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var weapon = held.AddComponent<PistolWeapon>();
        weapon.fireSound = fireSound;

        player.ShowMessage("Pistola obtenida. Clic izquierdo para disparar.", 4f);
        PlaySound();
        Destroy(gameObject);
    }
}