using UnityEngine;

// Poner en Tarjeta_Acceso.
public class CardPickup : Interactable
{
    void Reset() { prompt = "Pulsa E para recoger la tarjeta"; }

    public override void Interact(PlayerController player)
    {
        player.hasCard = true;
        player.ShowMessage("Tarjeta obtenida.", 3f);
        PlaySound();
        Destroy(gameObject);   // se recoge una sola vez
    }
}