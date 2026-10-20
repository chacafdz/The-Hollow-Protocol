using System.Collections.Generic;
using UnityEngine;

// Brujula en pantalla (barra superior con N, NE, E...). Se agrega sola al Player.
// Mientras no recojas la brujula, no se ve. Las pistas anaden marcadores de destino.
// Norte = eje +Z del mundo de Unity, Este = +X.
public class CompassHUD : MonoBehaviour
{
    public bool unlocked;
    public bool showDistance = true;

    class Marker { public string label; public Transform target; }

    readonly List<Marker> markers = new List<Marker>();
    static readonly string[] Names = { "N", "NE", "E", "SE", "S", "SO", "O", "NO" };
    GUIStyle style;

    public static CompassHUD Of(PlayerController player)
    {
        var hud = player.GetComponent<CompassHUD>();
        if (hud == null) hud = player.gameObject.AddComponent<CompassHUD>();
        return hud;
    }

    public void AddMarker(string label, Transform target)
    {
        if (target == null) return;
        foreach (var m in markers) if (m.target == target) return;
        markers.Add(new Marker { label = label, target = target });
    }

    public static string CardinalName(float degrees)
    {
        int i = Mathf.RoundToInt(Mathf.Repeat(degrees, 360f) / 45f) % 8;
        return Names[i];
    }

    // Ejemplo: "135° SE (~150 m)"
    public string Describe(Transform target)
    {
        Vector3 d = target.position - transform.position;
        float bearing = Mathf.Repeat(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 360f);
        float dist = new Vector2(d.x, d.z).magnitude;
        return Mathf.RoundToInt(bearing).ToString("000") + "° " + CardinalName(bearing)
             + " (~" + (Mathf.Round(dist / 10f) * 10f) + " m)";
    }

    void OnGUI()
    {
        if (!unlocked) return;

        GUI.depth = 5;
        float W = Screen.width * 0.6f;
        float Hh = Screen.height * 0.055f;
        float cx = Screen.width / 2f;
        float top = Screen.height * 0.07f;
        float yaw = transform.eulerAngles.y;

        if (style == null)
            style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Overflow };
        style.fontSize = Mathf.RoundToInt(Hh * 0.5f);

        // Fondo
        GUI.color = new Color(0f, 0f, 0f, 0.5f);
        GUI.DrawTexture(new Rect(cx - W / 2f, top, W, Hh), Texture2D.whiteTexture);

        // Marcas cada 15 grados y letras cada 45
        for (int a = 0; a < 360; a += 15)
        {
            float rel = Mathf.DeltaAngle(yaw, a);
            if (Mathf.Abs(rel) > 90f) continue;
            float x = cx + rel / 180f * W;
            bool major = a % 45 == 0;

            GUI.color = new Color(1f, 1f, 1f, major ? 0.9f : 0.45f);
            float tickH = major ? Hh * 0.35f : Hh * 0.18f;
            GUI.DrawTexture(new Rect(x - 1f, top + Hh - tickH, 2f, tickH), Texture2D.whiteTexture);

            if (major)
            {
                GUI.color = a == 0 ? new Color(1f, 0.4f, 0.3f) : Color.white;
                GUI.Label(new Rect(x - 30f, top + 1f, 60f, Hh * 0.65f), Names[a / 45], style);
            }
        }

        // Indicador central y rumbo en grados
        GUI.color = new Color(1f, 0.8f, 0.3f, 1f);
        GUI.DrawTexture(new Rect(cx - 1.5f, top, 3f, Hh), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(cx - 60f, top + Hh + 2f, 120f, Hh * 0.7f), Mathf.RoundToInt(yaw).ToString("000") + "°", style);

        // Marcadores de destino
        foreach (var m in markers)
        {
            if (m.target == null) continue;
            Vector3 d = m.target.position - transform.position;
            float bearing = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            float rel = Mathf.DeltaAngle(yaw, bearing);
            float dist = new Vector2(d.x, d.z).magnitude;
            float x = cx + Mathf.Clamp(rel, -90f, 90f) / 180f * W;
            bool visible = Mathf.Abs(rel) <= 90f;

            GUI.color = new Color(1f, 0.35f, 0.25f, 1f);
            GUI.DrawTexture(new Rect(x - 5f, top + Hh * 0.55f, 10f, 10f), Texture2D.whiteTexture);

            string txt = m.label + (showDistance ? " " + Mathf.RoundToInt(dist / 10f) * 10 + " m" : "");
            if (!visible) txt = (rel < 0 ? "< " : "") + txt + (rel > 0 ? " >" : "");
            GUI.Label(new Rect(x - 150f, top - Hh * 1.0f, 300f, Hh * 0.9f), txt, style);
        }

        GUI.color = Color.white;
    }
}
