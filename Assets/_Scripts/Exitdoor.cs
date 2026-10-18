using UnityEngine;

// Poner en Puerta_Salida. Se desliza hacia arriba al abrirse.
// En "Move With" arrastra las piezas que deben subir con la puerta (por ejemplo Puerta_Salida_franja).
public class ExitDoor : Interactable
{
    public float openHeight = 2.4f;
    public float openSpeed = 1.5f;
    public AudioClip finalSting;      // golpe fuerte o sonido repentino al terminar
    public Transform[] moveWith;      // piezas que suben junto con la puerta

    bool opened;
    Vector3 openPos;

    public override bool CanInteract => !opened;

    void Reset() { prompt = "Pulsa E para abrir"; }

    void Start()
    {
        openPos = transform.position + Vector3.up * openHeight;
    }

    public override void Interact(PlayerController player)
    {
        if (!player.hasCard)
        {
            player.ShowMessage("Acceso denegado. Se requiere tarjeta de mantenimiento.", 3.5f);
            return;
        }

        opened = true;

        // Quita colliders de la puerta y de sus piezas para que nunca bloqueen el paso
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (var t in moveWith)
            if (t != null)
                foreach (var c in t.GetComponentsInChildren<Collider>()) c.enabled = false;

        PlaySound();
        if (finalSting != null)
            AudioSource.PlayClipAtPoint(finalSting, transform.position);
        player.ShowMessage("Registro recuperado. Movimiento detectado detrás de ti…", 8f);
    }

    void Update()
    {
        if (!opened) return;

        Vector3 before = transform.position;
        transform.position = Vector3.MoveTowards(before, openPos, openSpeed * Time.deltaTime);
        Vector3 delta = transform.position - before;

        foreach (var t in moveWith)
            if (t != null) t.position += delta;
    }
}