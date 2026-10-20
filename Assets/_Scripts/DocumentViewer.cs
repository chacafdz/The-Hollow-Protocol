using UnityEngine;

/// <summary>
/// Visor de documentos estilo "archivos" de Resident Evil: al examinar una pista se pausa el juego,
/// la imagen del documento ocupa la pantalla y con T se alterna la transcripcion del texto.
/// Se cierra con E, Esc o clic derecho.
/// </summary>
public class DocumentViewer : MonoBehaviour
{
    /// <summary>True mientras hay un documento abierto.</summary>
    public static bool Open { get; private set; }

    /// <summary>Frame en que se cerro el ultimo documento (para que la misma tecla no abra la pausa).</summary>
    public static int ClosedFrame { get; private set; } = -10;

    static DocumentViewer inst;

    Texture2D image;
    string title;
    string transcript;
    bool showText;
    System.Action closedCallback;
    float scroll;          // desplazamiento de la transcripcion en pixeles
    float maxScroll;       // se calcula en OnGUI segun el largo del texto
    int openedFrame;
    int reenableFrame = -1;
    float openTime;
    float prevScale = 1f;
    bool playerWasEnabled;
    PlayerController player;
    GUIStyle titleStyle, textStyle, hintStyle;

    /// <summary>Abre un documento.</summary>
    /// <param name="p">Jugador que lo examina.</param>
    /// <param name="img">Imagen del documento.</param>
    /// <param name="docTitle">Titulo que se muestra arriba.</param>
    /// <param name="text">Transcripcion (se ve con T). Puede ir vacia.</param>
    /// <param name="onClosed">Accion opcional al cerrar el documento.</param>
    public static void Show(PlayerController p, Texture2D img, string docTitle, string text, System.Action onClosed = null)
    {
        if (inst == null) inst = new GameObject("DocumentViewer").AddComponent<DocumentViewer>();
        inst.Begin(p, img, docTitle, text);
        inst.closedCallback = onClosed;
        if (img == null) inst.showText = true;   // sin imagen: se muestra el texto directamente
    }

    void Begin(PlayerController p, Texture2D img, string docTitle, string text)
    {
        image = img;
        title = docTitle;
        transcript = text;
        scroll = 0f;
        showText = false;
        player = p;
        playerWasEnabled = p != null && p.enabled;
        if (p != null) p.enabled = false;
        prevScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        openedFrame = Time.frameCount;
        openTime = Time.unscaledTime;
        Open = true;
    }

    void Close()
    {
        Open = false;
        ClosedFrame = Time.frameCount;
        Time.timeScale = prevScale;
        // el jugador se reactiva el siguiente frame, asi la misma E no vuelve a abrir el documento
        reenableFrame = Time.frameCount + 1;
        var cb = closedCallback;
        closedCallback = null;
        cb?.Invoke();
    }

    void Update()
    {
        if (reenableFrame >= 0 && Time.frameCount >= reenableFrame)
        {
            reenableFrame = -1;
            if (player != null && playerWasEnabled && !PlayerHealth.Dead) player.enabled = true;
        }

        if (!Open || Time.frameCount == openedFrame) return;

        if (Input.GetKeyDown(KeyCode.T) && !string.IsNullOrEmpty(transcript) && image != null) { showText = !showText; scroll = 0f; }

        // desplazar el texto: S / flecha abajo, W / flecha arriba, o la rueda del raton
        if (showText)
        {
            float speed = Screen.height * 0.9f * Time.unscaledDeltaTime;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) scroll += speed;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) scroll -= speed;
            scroll -= Input.mouseScrollDelta.y * Screen.height * 0.06f;
            scroll = Mathf.Clamp(scroll, 0f, maxScroll);
        }
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            Close();
    }

    void OnDestroy()
    {
        if (inst == this) { inst = null; Open = false; }
    }

    void OnGUI()
    {
        if (!Open) return;
        float W = Screen.width, H = Screen.height;
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            textStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, wordWrap = true };
            hintStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        }
        titleStyle.fontSize = Mathf.RoundToInt(H * 0.038f);
        textStyle.fontSize = Mathf.RoundToInt(H * 0.028f);
        hintStyle.fontSize = Mathf.RoundToInt(H * 0.022f);

        GUI.depth = -200;
        float k = Mathf.Clamp01((Time.unscaledTime - openTime) / 0.18f);   // pequena animacion de entrada

        // fondo
        GUI.color = new Color(0f, 0f, 0f, 0.88f * k);
        GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture);

        // titulo
        GUI.color = new Color(0.9f, 0.85f, 0.7f, k);
        GUI.Label(new Rect(0, H * 0.02f, W, H * 0.07f), title, titleStyle);

        // documento
        if (image != null)
        {
            float maxW = W * (showText ? 0.45f : 0.8f), maxH = H * 0.78f;
            float aspect = image.width / (float)image.height;
            float h = maxH, w = h * aspect;
            if (w > maxW) { w = maxW; h = w / aspect; }
            float s = Mathf.Lerp(0.92f, 1f, k);
            w *= s; h *= s;
            float cx = showText ? W * 0.27f : W * 0.5f;
            GUI.color = new Color(1f, 1f, 1f, showText ? 0.55f : k);
            GUI.DrawTexture(new Rect(cx - w / 2f, H * 0.5f - h / 2f + H * 0.02f, w, h), image, ScaleMode.ScaleToFit);
        }

        // transcripcion
        if (showText)
        {
            Rect box = new Rect(W * 0.52f, H * 0.12f, W * 0.43f, H * 0.76f);
            GUI.color = new Color(0.08f, 0.07f, 0.06f, 0.92f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = new Color(0.92f, 0.9f, 0.85f, 1f);
            Rect view = new Rect(box.x + 24f, box.y + 20f, box.width - 48f, box.height - 40f);
            float textH = textStyle.CalcHeight(new GUIContent(transcript), view.width);
            maxScroll = Mathf.Max(0f, textH - view.height);
            scroll = Mathf.Clamp(scroll, 0f, maxScroll);
            GUI.BeginGroup(view);   // recorta el texto a la caja
            GUI.Label(new Rect(0f, -scroll, view.width, textH), transcript, textStyle);
            GUI.EndGroup();

            // barra de desplazamiento
            if (maxScroll > 0f)
            {
                float barH = view.height * view.height / textH;
                float barY = view.y + (view.height - barH) * (scroll / maxScroll);
                GUI.color = new Color(0.9f, 0.85f, 0.7f, 0.25f);
                GUI.DrawTexture(new Rect(box.xMax - 10f, view.y, 4f, view.height), Texture2D.whiteTexture);
                GUI.color = new Color(0.9f, 0.85f, 0.7f, 0.9f);
                GUI.DrawTexture(new Rect(box.xMax - 11f, barY, 6f, barH), Texture2D.whiteTexture);
            }
        }

        // ayudas
        GUI.color = new Color(0.75f, 0.75f, 0.75f, k);
        string hint = "[E] / [Esc]  Cerrar";
        if (!string.IsNullOrEmpty(transcript) && image != null) hint = (showText ? "[T]  Ver documento" : "[T]  Leer texto") + "        " + hint;
        if (showText && maxScroll > 0f) hint = "[W/S] o rueda  Desplazar        " + hint;
        GUI.Label(new Rect(0, H * 0.92f, W, H * 0.06f), hint, hintStyle);
        GUI.color = Color.white;
    }
}
