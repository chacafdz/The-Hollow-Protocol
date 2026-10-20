using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Niebla nocturna del juego. Se aplica sola al cargar cualquier nivel (no en el menu).
/// Para cambiarla, agrega este componente a un objeto de la escena y ajusta sus valores.
/// </summary>
public class GameFog : MonoBehaviour
{
    /// <summary>Color de la niebla: un azul grisaceo claro destaca contra el cielo negro de la noche.</summary>
    public Color fogColor = new Color(0.11f, 0.14f, 0.19f);

    /// <summary>Densidad de la niebla. Mas alto = se ve menos lejos.</summary>
    [Range(0f, 0.08f)] public float density = 0.02f;

    /// <summary>Tipo de niebla.</summary>
    public FogMode mode = FogMode.Exponential;

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
        if (FindFirstObjectByType<GameFog>() == null) new GameObject("GameFog").AddComponent<GameFog>();
    }

    void Start()
    {
        Apply();
    }

    /// <summary>Activa la niebla en la configuracion de iluminacion de la escena.</summary>
    public void Apply()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = mode;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = density;
    }
}
