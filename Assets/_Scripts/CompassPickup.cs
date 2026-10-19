using UnityEngine;

// Poner en el grupo "Brujula" (antes agrega un Box Collider de unos 0.3 en cada eje).
public class CompassPickup : Interactable
{
    [TextArea] public string message = "Brújula obtenida. Las pistas te dirán hacia dónde ir.";

    void Reset() { prompt = "Pulsa E para recoger la brújula"; }

    public override void Interact(PlayerController player)
    {
        CompassHUD.Of(player).unlocked = true;
        player.ShowMessage(message, 4f);
        PlaySound();
        Destroy(gameObject);
    }
}
