using UnityEngine;

// Poner en Terminal_Pantalla.
public class TerminalLog : Interactable
{
    [TextArea(2, 5)]
    public string logText = "Registro parcial: el organismo continúa respondiendo a estímulos.";
    public float showSeconds = 7f;

    void Reset() { prompt = "Pulsa E para leer el registro"; }

    public override void Interact(PlayerController player)
    {
        player.ShowMessage(logText, showSeconds);
        PlaySound();
    }
}