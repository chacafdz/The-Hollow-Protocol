using UnityEngine;

// Pista examinable: diario, foto, nota, mapa, radio... Muestra su texto y cuenta cuantas has encontrado.
// Si "Points To" tiene un destino, la pista lo marca en la brujula y anade el rumbo al mensaje.
// Mantén los textos cortos (2 o 3 lineas) para que quepan en pantalla.
public class ClueNote : Interactable
{
    [TextArea(2, 6)] public string clueText = "Escribe aqui el texto de la pista.";
    public float showSeconds = 8f;
    public bool countsAsClue = true;

    [Header("Documento (opcional, estilo Resident Evil)")]
    /// <summary>Imagen del documento. Si hay una, al examinar se abre a pantalla completa.</summary>
    public Texture2D documentImage;
    /// <summary>Titulo del documento en el visor.</summary>
    public string documentTitle = "";
    /// <summary>Transcripcion del texto del documento (se ve con T).</summary>
    [TextArea(4, 14)] public string transcript = "";

    [Header("Brujula (opcional)")]
    public Transform pointsTo;                    // lugar que esta pista senala (casa, cabana...)
    public string pointsToLabel = "Destino";
    public bool appendBearing = true;             // anade "135° SE (~150 m)" al mensaje

    bool read;

    void Reset() { prompt = "Pulsa E para examinar"; }

    public override void Interact(PlayerController player)
    {
        string prefix = "";
        string suffix = "";

        if (countsAsClue)
        {
            var inv = Inventory.Of(player);
            if (!read) { read = true; inv.cluesFound++; }

            int total = 0;
            foreach (var c in FindObjectsByType<ClueNote>(FindObjectsSortMode.None))
                if (c.countsAsClue) total++;
            prefix = "[Pista " + inv.cluesFound + "/" + total + "] ";
        }

        if (pointsTo != null)
        {
            var hud = CompassHUD.Of(player);
            hud.AddMarker(pointsToLabel, pointsTo);
            if (appendBearing)
            {
                suffix = "\n" + pointsToLabel + ": " + hud.Describe(pointsTo);
                if (!hud.unlocked) suffix += " — necesitas una brújula";
            }
        }

        // documento: primero el catalogo por nombre (Pista_Mapa, Pista_Foto...), si no, lo asignado en el Inspector
        Texture2D img = documentImage;
        string title = string.IsNullOrEmpty(documentTitle) ? name : documentTitle;
        string text = transcript;
        string key = DocumentLibrary.KeyFor(transform);
        if (key != null && DocumentLibrary.TryGet(key, out var libImg, out var libTitle, out var libText))
        {
            img = libImg; title = libTitle; text = libText;
        }

        if (img != null)
        {
            DocumentViewer.Show(player, img, title, text);
            // al cerrar solo queda un aviso corto; el texto completo ya se leyo en el visor
            player.ShowMessage(prefix + title + suffix, suffix.Length > 0 ? showSeconds : 2.5f);
        }
        else player.ShowMessage(prefix + clueText + suffix, showSeconds);
        PlaySound();
    }
}
