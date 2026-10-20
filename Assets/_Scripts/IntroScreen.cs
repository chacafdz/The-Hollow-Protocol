using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pantalla de introduccion del nivel: historia, objetivo y controles.
/// Se muestra al empezar la partida y se puede volver a abrir con Tab.
/// Se crea sola en cualquier escena cuyo nombre no contenga "menu".
/// </summary>
public class IntroScreen : MonoBehaviour
{
    /// <summary>True mientras la pantalla esta abierta.</summary>
    public static bool Open { get; private set; }

    /// <summary>Titulo de la pantalla.</summary>
    public string title = "THE HOLLOW PROTOCOL";

    /// <summary>Historia del juego.</summary>
    [TextArea(4, 12)] public string backstory =
        "Hace años, la corporación CRAYO financió un laboratorio aislado en medio del bosque para investigar la biología sintética. " +
        "El experimento no fracasó: dio origen a algo que se adapta, escucha y caza. " +
        "CRAYO borró el lugar de sus registros y lo selló. Hoy, una señal de auxilio salió de ahí.\n\n" +
        "Despiertas dentro del laboratorio, sin comunicación con el exterior. " +
        "La única prueba de lo que pasó es un registro escondido en una cabaña, en lo profundo del bosque.";

    /// <summary>Lo que hay que lograr.</summary>
    [TextArea(4, 12)] public string objective =
        "OBJETIVO: llega a la cabaña y recupera el registro.\n" +
        "1. Lee la terminal del laboratorio y sal.\n" +
        "2. Sigue la luz roja hasta la torre: ahí hay una brújula y una llave.\n" +
        "3. Revisa la casa vieja: la radio y el mapa te indican el camino.\n" +
        "4. Abre la cabaña con la llave y toma el registro.\n\n" +
        "Algo te caza en el bosque. No puedes matarlo: los disparos solo lo ahuyentan, y la munición es escasa.";

    /// <summary>Controles.</summary>
    [TextArea(2, 6)] public string controls =
        "WASD mover · Shift correr · Espacio saltar · Ratón mirar · E interactuar · F linterna · R recargar · Esc pausa · Tab ver esta pantalla";

    float prevScale = 1f;
    bool playerWasEnabled;
    bool firstFrame = true;
    PlayerController player;
    GUIStyle titleStyle, bodyStyle, hintStyle, objStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
        Ensure(SceneManager.GetActiveScene());
    }

    static void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        Ensure(scene);
    }

    static void Ensure(Scene scene)
    {
        Open = false;
        if (scene.name.ToLowerInvariant().Contains("menu")) return;
        if (FindFirstObjectByType<IntroScreen>() == null) new GameObject("IntroScreen").AddComponent<IntroScreen>();
    }

    void Update()
    {
        if (firstFrame)
        {
            firstFrame = false;
            Show();
            return;
        }

        if (Open)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Tab))
                Hide();
        }
        else if (Input.GetKeyDown(KeyCode.Tab) && Time.timeScale > 0f && !PlayerHealth.Dead && !DocumentViewer.Open)
        {
            Show();
        }
    }

    void Show()
    {
        player = FindFirstObjectByType<PlayerController>();
        playerWasEnabled = player != null && player.enabled;
        if (player != null) player.enabled = false;
        prevScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Open = true;
    }

    void Hide()
    {
        Open = false;
        Time.timeScale = prevScale;
        if (player != null && playerWasEnabled)
        {
            player.enabled = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void OnDestroy()
    {
        Open = false;
    }

    void OnGUI()
    {
        if (!Open) return;
        float h = Screen.height;
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            bodyStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, wordWrap = true };
            objStyle = new GUIStyle(bodyStyle);
            hintStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
        }
        titleStyle.fontSize = Mathf.RoundToInt(h * 0.06f);
        bodyStyle.fontSize = Mathf.RoundToInt(h * 0.026f);
        objStyle.fontSize = Mathf.RoundToInt(h * 0.026f);
        hintStyle.fontSize = Mathf.RoundToInt(h * 0.022f);

        GUI.depth = -100;
        GUI.color = new Color(0f, 0f, 0f, 0.93f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

        float w = Screen.width * 0.68f;
        float x = (Screen.width - w) / 2f;
        float y = h * 0.05f;

        GUI.color = new Color(0.9f, 0.25f, 0.2f);
        GUI.Label(new Rect(x, y, w, h * 0.09f), title, titleStyle);
        y += h * 0.11f;

        GUI.color = new Color(0.85f, 0.85f, 0.85f);
        float bh = bodyStyle.CalcHeight(new GUIContent(backstory), w);
        GUI.Label(new Rect(x, y, w, bh), backstory, bodyStyle);
        y += bh + h * 0.03f;

        GUI.color = new Color(1f, 0.85f, 0.4f);
        float oh = objStyle.CalcHeight(new GUIContent(objective), w);
        GUI.Label(new Rect(x, y, w, oh), objective, objStyle);
        y += oh + h * 0.03f;

        GUI.color = new Color(0.6f, 0.75f, 0.9f);
        float ch = hintStyle.CalcHeight(new GUIContent(controls), w);
        GUI.Label(new Rect(x, y, w, ch), controls, hintStyle);

        var btn = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(h * 0.032f) };
        float bw = Screen.width * 0.2f, bth = h * 0.07f;
        GUI.color = Color.white;
        if (GUI.Button(new Rect((Screen.width - bw) / 2f, Mathf.Min(y + ch + h * 0.03f, h - bth - h * 0.03f), bw, bth), "Comenzar", btn))
            Hide();
    }
}
