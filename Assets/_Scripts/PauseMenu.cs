using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu de pausa del nivel: se abre y se cierra con Esc. Continuar, Reiniciar, Volver al menu y volumen.
/// Se dibuja con IMGUI (igual que la pantalla final), asi no depende de un EventSystem.
/// Se crea solo en cualquier escena cuyo nombre no contenga "menu".
/// </summary>
public class PauseMenu : MonoBehaviour
{
    /// <summary>Nombre de la escena del menu principal.</summary>
    public const string MenuScene = MainMenu.MenuSceneName;

    /// <summary>True mientras el juego esta en pausa.</summary>
    public static bool Paused { get; private set; }

    PlayerController player;
    bool playerWasEnabled;
    float volume;
    GUIStyle titleStyle, btnStyle, labelStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
        AudioListener.volume = PlayerPrefs.GetFloat(MainMenu.VolumeKey, 1f);
        Ensure(SceneManager.GetActiveScene());
    }

    static void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        Ensure(scene);
    }

    static void Ensure(Scene scene)
    {
        Paused = false;
        if (scene.name.ToLowerInvariant().Contains("menu")) return;
        if (FindFirstObjectByType<PauseMenu>() == null) new GameObject("PauseMenu").AddComponent<PauseMenu>();
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (PlayerHealth.Dead || IntroScreen.Open || DocumentViewer.Open) return;
        if (Time.frameCount - DocumentViewer.ClosedFrame <= 1) return;   // ese Esc era para cerrar el documento
        if (Paused) Resume(); else Pause();
    }

    /// <summary>Detiene el juego y muestra el menu de pausa.</summary>
    public void Pause()
    {
        player = FindFirstObjectByType<PlayerController>();
        playerWasEnabled = player != null && player.enabled;
        if (player != null) player.enabled = false;

        volume = AudioListener.volume;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Paused = true;
    }

    /// <summary>Cierra el menu y reanuda el juego.</summary>
    public void Resume()
    {
        Paused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (player != null && !PlayerHealth.Dead)
        {
            player.enabled = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    /// <summary>Cambia el volumen general y lo guarda.</summary>
    /// <param name="value">Volumen de 0 a 1.</param>
    public void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(MainMenu.VolumeKey, value);
    }

    void Restart()
    {
        ResetTime();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void ToMenu()
    {
        ResetTime();
        SceneManager.LoadScene(MenuScene);
    }

    static void ResetTime()
    {
        Paused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    void OnDestroy()
    {
        if (Paused) ResetTime();
    }

    void OnGUI()
    {
        if (!Paused) return;
        float W = Screen.width, H = Screen.height;
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            btnStyle = new GUIStyle(GUI.skin.button);
            labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        }
        titleStyle.fontSize = Mathf.RoundToInt(H * 0.07f);
        btnStyle.fontSize = Mathf.RoundToInt(H * 0.032f);
        labelStyle.fontSize = Mathf.RoundToInt(H * 0.03f);

        GUI.depth = -300;   // encima de todo
        GUI.color = new Color(0f, 0f, 0f, 0.78f);
        GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(0, H * 0.14f, W, H * 0.1f), "PAUSA", titleStyle);

        float bw = W * 0.24f, bh = H * 0.075f, x = (W - bw) / 2f, y = H * 0.3f, gap = bh * 1.3f;
        if (GUI.Button(new Rect(x, y, bw, bh), "Continuar", btnStyle)) Resume();
        if (GUI.Button(new Rect(x, y + gap, bw, bh), "Reiniciar", btnStyle)) Restart();
        if (GUI.Button(new Rect(x, y + gap * 2f, bw, bh), "Menú principal", btnStyle)) ToMenu();

        // volumen (se guarda y vale tambien para el menu principal)
        float vy = y + gap * 3.3f;
        GUI.Label(new Rect(0, vy, W, H * 0.05f), "Volumen: " + Mathf.RoundToInt(volume * 100f) + "%", labelStyle);
        float sw = W * 0.3f;
        float nv = GUI.HorizontalSlider(new Rect((W - sw) / 2f, vy + H * 0.06f, sw, H * 0.03f), volume, 0f, 1f);
        if (!Mathf.Approximately(nv, volume)) { volume = nv; SetVolume(nv); }

        GUI.Label(new Rect(0, H * 0.9f, W, H * 0.05f), "[Esc]  Continuar", labelStyle);
    }
}
