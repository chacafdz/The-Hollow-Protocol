using UnityEngine;

// Poner en Llave_Cabana (antes agrega un Box Collider al mismo objeto, un poco grande: 0.4 en cada eje).
/// <summary>Llave que el jugador recoge y se guarda en su <see cref="Inventory"/>.</summary>
public class KeyPickup : Interactable
{
    /// <summary>Identificador de la llave (coincide con <c>LockedDoor.requiredKey</c>).</summary>
    public string keyId = "cabana";
    [TextArea] public string message = "Llave de la cabaña obtenida.";

    void Reset() { prompt = "Pulsa E para recoger la llave"; }

    public override void Interact(PlayerController player)
    {
        Inventory.Of(player).AddKey(keyId);
        player.ShowMessage(message, 3.5f);
        PlaySound();
        Destroy(gameObject);
    }
}
