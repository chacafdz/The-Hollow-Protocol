using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu principal del juego: Jugar, Opciones (volumen) y Salir.
/// Construye su propia interfaz por codigo al iniciar, asi la escena solo necesita este componente.
/// </summary>
public class MainMenu : MonoBehaviour
{
    /// <summary>Nombre de la escena del nivel (debe estar en Build Settings).</summary>
    public string levelScene = "SampleScene";

    /// <summary>Musica ambiental del menu (opcional).</summary>
    public AudioClip menuMusic;

    /// <summary>Nombre de la escena del menu principal (debe estar en Build Settings).</summary>
    public const string MenuSceneName = "Main Menu";

    /// <summary>Clave de PlayerPrefs donde se guarda el volumen.</summary>
    public const string VolumeKey = "volumen";

    GameObject mainPanel;
    GameObject optionsPanel;
    Text volumeText;

    void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f);

        if (Camera.allCamerasCount == 0)
        {
            var camGo = new GameObject("Camara del menu", typeof(Camera), typeof(AudioListener));
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f);
        }

        UIKit.EnsureEventSystem();
        BuildUI();

        if (menuMusic != null)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = menuMusic;
            src.loop = true;
            src.volume = 0.6f;
            src.Play();
        }
    }

    void BuildUI()
    {
        var canvas = UIKit.CreateCanvas("MenuCanvas", 0);
        UIKit.Panel(canvas.transform, "Fondo", new Color(0.02f, 0.02f, 0.03f, 1f));

        // ----- panel principal
        mainPanel = new GameObject("PanelPrincipal", typeof(RectTransform));
        mainPanel.transform.SetParent(canvas.transform, false);
        Stretch(mainPanel);

        UIKit.Label(mainPanel.transform, "THE HOLLOW PROTOCOL", 64, new Vector2(0f, 190f), new Vector2(1000f, 90f), TextAnchor.MiddleCenter, new Color(0.85f, 0.15f, 0.15f));
        UIKit.Label(mainPanel.transform, "Survival horror", 26, new Vector2(0f, 130f), new Vector2(600f, 40f), TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f));

        UIKit.Button(mainPanel.transform, "Jugar", new Vector2(0f, 40f), new Vector2(300f, 56f), Play);
        UIKit.Button(mainPanel.transform, "Opciones", new Vector2(0f, -30f), new Vector2(300f, 56f), ShowOptions);
        UIKit.Button(mainPanel.transform, "Salir", new Vector2(0f, -100f), new Vector2(300f, 56f), Quit);

        UIKit.Label(mainPanel.transform,
            "WASD mover  ·  Shift correr  ·  E interactuar  ·  F linterna  ·  Clic disparar  ·  Esc pausa",
            18, new Vector2(0f, -200f), new Vector2(1100f, 30f), TextAnchor.MiddleCenter, new Color(0.6f, 0.6f, 0.6f));
        UIKit.Label(mainPanel.transform,
            "Modelo del monstruo: \"Smily horror monster\" by Bento (CC BY 4.0)",
            14, new Vector2(0f, -310f), new Vector2(1100f, 24f), TextAnchor.MiddleCenter, new Color(0.45f, 0.45f, 0.45f));

        // ----- panel de opciones
        optionsPanel = new GameObject("PanelOpciones", typeof(RectTransform));
        optionsPanel.transform.SetParent(canvas.transform, false);
        Stretch(optionsPanel);

        UIKit.Label(optionsPanel.transform, "OPCIONES", 48, new Vector2(0f, 150f), new Vector2(600f, 70f));
        float vol = AudioListener.volume;
        volumeText = UIKit.Label(optionsPanel.transform, "Volumen: " + Mathf.RoundToInt(vol * 100f) + "%", 28, new Vector2(0f, 60f), new Vector2(500f, 40f));
        UIKit.Slider(optionsPanel.transform, new Vector2(0f, 10f), new Vector2(400f, 30f), vol, SetVolume);
        UIKit.Button(optionsPanel.transform, "Volver", new Vector2(0f, -80f), new Vector2(300f, 56f), ShowMain);
        optionsPanel.SetActive(false);
    }

    static void Stretch(GameObject go)
    {
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    /// <summary>Carga el nivel del juego.</summary>
    public void Play()
    {
        SceneManager.LoadScene(levelScene);
    }

    /// <summary>Muestra el panel de opciones.</summary>
    public void ShowOptions()
    {
        mainPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    /// <summary>Vuelve al panel principal.</summary>
    public void ShowMain()
    {
        optionsPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    /// <summary>Cambia el volumen general y lo guarda.</summary>
    /// <param name="value">Volumen de 0 a 1.</param>
    public void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
        if (volumeText != null) volumeText.text = "Volumen: " + Mathf.RoundToInt(value * 100f) + "%";
    }

    /// <summary>Cierra el juego (en el editor detiene el modo Play).</summary>
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
