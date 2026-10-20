using UnityEngine;

// Poner en Registro_Final (la cabana). Al tomarlo se lee el registro y despues se dispara el final del juego.
// En EndTrigger desmarca "Trigger By Entering" para que el final solo ocurra aqui.
/// <summary>
/// Registro final sobre la mesa de la cabana. Al tomarlo el jugador queda inmovil, se abre el documento
/// del registro (estilo Resident Evil) y al cerrarlo empieza el final con el jump scare.
/// </summary>
public class RegistroPickup : Interactable
{
    /// <summary>Disparador del final que se activa al cerrar el registro.</summary>
    public EndTrigger endTrigger;
    bool taken;

    public override bool CanInteract => !taken;

    void Reset() { prompt = "Pulsa E para tomar el registro"; }

    public override void Interact(PlayerController player)
    {
        taken = true;
        PlaySound();

        // desde aqui el jugador ya no se mueve: el jump scare ocurre frente a la mesa
        player.frozen = true;

        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;

        DocumentLibrary.TryGet("Registro", out var img, out var title, out var text);
        DocumentViewer.Show(player, img, title, text, () => StartEnding(player));
    }

    void StartEnding(PlayerController player)
    {
        if (endTrigger == null) endTrigger = FindFirstObjectByType<EndTrigger>();
        if (endTrigger != null)
        {
            if (endTrigger.scareSpot == null) endTrigger.scareSpot = transform;   // el enemigo aparece sobre la mesa del registro
            endTrigger.Begin(player);
        }
        else Debug.LogWarning("RegistroPickup: no hay EndTrigger en la escena, el final no se dispara.");
    }
}
