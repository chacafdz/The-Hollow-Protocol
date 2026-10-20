using UnityEngine;

/// <summary>
/// Caja de municion: al recogerla con E suma balas a la reserva del <see cref="Inventory"/>.
/// La pistola las usa al recargar (tecla R).
/// </summary>
public class AmmoPickup : Interactable
{
    /// <summary>Balas que da la caja.</summary>
    public int amount = 6;

    void Reset() { prompt = "Pulsa E para recoger munición"; }

    /// <summary>Suma las balas a la reserva del jugador y destruye la caja.</summary>
    /// <param name="player">Jugador que interactua.</param>
    public override void Interact(PlayerController player)
    {
        var inv = Inventory.Of(player);
        inv.reserveAmmo += amount;
        player.ShowMessage("+" + amount + " balas. Reserva: " + inv.reserveAmmo + ". Pulsa R para recargar.", 3.5f);
        PlaySound();
        Destroy(gameObject);
    }
}
