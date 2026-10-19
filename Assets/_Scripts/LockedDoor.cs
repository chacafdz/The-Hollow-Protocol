using UnityEngine;

// Puerta con bisagra. Sirve para la casa vieja (sin llave) y para la cabana (con llave).
// Poner en la malla de la puerta (Puerta_Casa o la de la cabana) y arrastrar el locator de la bisagra.
/// <summary>
/// Puerta con bisagra que puede requerir una llave. Sirve para la casa vieja (sin llave)
/// y para la cabana (con llave). Las piezas sueltas (pomo, candado) giran junto con ella.
/// </summary>
public class LockedDoor : Interactable
{
    [Header("Cerradura")]
    /// <summary>Identificador de la llave necesaria. Vacio = sin llave.</summary>
    public string requiredKey = "";              // vacio = sin llave; "cabana" = necesita la llave de la cabana
    [TextArea] public string lockedMessage = "Cerrada. Necesitas una llave.";
    [TextArea] public string openMessage = "";

    [Header("Giro")]
    /// <summary>Punto de la bisagra: la puerta gira alrededor de el.</summary>
    public Transform hinge;                      // locator en el borde de la bisagra
    /// <summary>Angulo de apertura en grados. Negativo = abre hacia el otro lado.</summary>
    public float openAngle = 100f;               // usa un valor negativo para abrir hacia el otro lado
    public float openSpeed = 90f;
    [Tooltip("Pomo, herraje, candado... piezas sueltas que deben girar junto con la puerta")]
    public Transform[] attachedParts;

    bool opened;
    float turned;

    public override bool CanInteract => !opened;

    void Reset() { prompt = "Pulsa E para abrir"; }

    /// <summary>Abre la puerta si el jugador tiene la llave; si no, muestra el mensaje de cerrada.</summary>
    /// <param name="player">Jugador que interactua.</param>
    public override void Interact(PlayerController player)
    {
        if (!Inventory.Of(player).HasKey(requiredKey))
        {
            player.ShowMessage(lockedMessage, 3.5f);
            return;
        }

        opened = true;
        PlaySound();
        if (!string.IsNullOrEmpty(openMessage)) player.ShowMessage(openMessage, 4f);

        // La puerta abierta nunca debe bloquear el paso
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
    }

    void Update()
    {
        float target = Mathf.Abs(openAngle);
        if (!opened || turned >= target) return;

        float step = Mathf.Min(openSpeed * Time.deltaTime, target - turned);
        Vector3 pivot = hinge != null ? hinge.position : transform.position;
        float signed = step * Mathf.Sign(openAngle);
        transform.RotateAround(pivot, Vector3.up, signed);
        if (attachedParts != null)
            foreach (var part in attachedParts)
                if (part != null && !part.IsChildOf(transform)) part.RotateAround(pivot, Vector3.up, signed);
        turned += step;
    }
}
