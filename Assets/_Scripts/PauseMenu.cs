using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu de pausa del nivel: se abre con Esc. Continuar, Reiniciar y Volver al menu principal.
/// Se crea solo en cualquier escena cuyo nombre no contenga "menu".
/// </summary>
public class PauseMenu : MonoBehaviour
{
    /// <summary>Nombre de la escena del menu principal.</summary>
    public const string MenuScene = "MainMenu";

    GameObject root;
    PlayerController player;
    bool paused;
    bool playerWasEnabled;

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
        if (scene.name.ToLowerInvariant().Contains("menu")) return;
        if (FindFirstObjectByType<PauseMenu>() == null) new GameObject("PauseMenu").AddComponent<PauseMenu>();
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (paused) Resume(); else Pause();
    }

    /// <summary>Detiene el juego y muestra el menu de pausa.</summary>
    public void Pause()
    {
        if (root == null) Build();

        player = FindFirstObjectByType<PlayerController>();
        playerWasEnabled = player != null && player.enabled;
        if (player != null) player.enabled = false;

        Time.timeScale = 0f;
        AudioListener.pause = true;
        root.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        paused = true;
    }

    /// <summary>Cierra el menu y reanuda el juego.</summary>
    public void Resume()
    {
        if (root != null) root.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        paused = false;

        if (player != null && playerWasEnabled)
        {
            player.enabled = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
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
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    void OnDestroy()
    {
        if (paused) ResetTime();
    }

    void Build()
    {
        UIKit.EnsureEventSystem();
        var canvas = UIKit.CreateCanvas("PausaCanvas", 100);
        root = canvas.gameObject;
        root.transform.SetParent(transform, false);

        UIKit.Panel(canvas.transform, "Fondo", new Color(0f, 0f, 0f, 0.75f));
        UIKit.Label(canvas.transform, "PAUSA", 56, new Vector2(0f, 150f), new Vector2(600f, 80f));
        UIKit.Button(canvas.transform, "Continuar", new Vector2(0f, 50f), new Vector2(300f, 56f), Resume);
        UIKit.Button(canvas.transform, "Reiniciar", new Vector2(0f, -20f), new Vector2(300f, 56f), Restart);
        UIKit.Button(canvas.transform, "Menu principal", new Vector2(0f, -90f), new Vector2(300f, 56f), ToMenu);
        root.SetActive(false);
    }
}
