using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Vida del jugador. Dibuja la barra de vida (abajo a la izquierda), el contador de balas
/// (abajo a la derecha), un destello rojo al recibir dano y la pantalla de muerte.
/// Se agrega solo al jugador en cualquier nivel (no en el menu).
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    /// <summary>Vida maxima del jugador.</summary>
    public float maxHealth = 100f;

    /// <summary>Vida actual.</summary>
    public float CurrentHealth { get; private set; }

    /// <summary>Verdadero si el jugador murio.</summary>
    public bool IsDead { get; private set; }

    /// <summary>Verdadero si el jugador de la escena actual murio (lo consulta el menu de pausa).</summary>
    public static bool Dead { get; private set; }

    PlayerController player;
    RectTransform fill;
    Image fillImage;
    Text healthText;
    Text ammoText;
    Image flash;
    GameObject deathPanel;
    float flashAlpha;
    float nextScan;
    PistolWeapon weapon;

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
        Dead = false;
        if (scene.name.ToLowerInvariant().Contains("menu")) return;
        var p = FindFirstObjectByType<PlayerController>();
        if (p != null) Of(p);
    }

    /// <summary>Devuelve la vida del jugador; la crea si no existe.</summary>
    /// <param name="p">Jugador.</param>
    public static PlayerHealth Of(PlayerController p)
    {
        var h = p.GetComponent<PlayerHealth>();
        if (h == null) h = p.gameObject.AddComponent<PlayerHealth>();
        return h;
    }

    void Awake()
    {
        player = GetComponent<PlayerController>();
        CurrentHealth = maxHealth;
        IsDead = false;
        Dead = false;
        BuildHud();
        Refresh();
    }

    // ---------- dano y curacion

    /// <summary>Resta vida. Si llega a cero, el jugador muere.</summary>
    /// <param name="amount">Cantidad de dano.</param>
    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        flashAlpha = 0.55f;
        Refresh();
        if (CurrentHealth <= 0f) Die();
    }

    /// <summary>Recupera vida sin pasar del maximo.</summary>
    /// <param name="amount">Cantidad de vida.</param>
    public void Heal(float amount)
    {
        if (IsDead) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        Refresh();
    }

    void Die()
    {
        IsDead = true;
        Dead = true;
        if (player != null) player.enabled = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (deathPanel == null) BuildDeathPanel();
        deathPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    // ---------- interfaz

    void BuildHud()
    {
        var canvas = UIKit.CreateCanvas("HUD", 5);
        canvas.transform.SetParent(transform, false);

        // destello rojo al recibir dano
        flash = UIKit.Panel(canvas.transform, "Destello", new Color(0.7f, 0f, 0f, 0f)).GetComponent<Image>();
        flash.raycastTarget = false;

        // barra de vida (abajo a la izquierda)
        var bar = MakeRect("BarraVida", canvas.transform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(30f, 30f), new Vector2(340f, 34f));
        bar.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

        var f = MakeRect("Relleno", bar, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        f.offsetMin = new Vector2(3f, 3f);
        f.offsetMax = new Vector2(-3f, -3f);
        fillImage = f.gameObject.AddComponent<Image>();
        fillImage.color = new Color(0.75f, 0.1f, 0.1f);
        fill = f;

        healthText = UIKit.Label(bar, "VIDA", 20, Vector2.zero, new Vector2(340f, 34f));
        var trt = (RectTransform)healthText.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        // contador de balas (abajo a la derecha)
        var ammo = MakeRect("Balas", canvas.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 30f), new Vector2(360f, 44f));
        ammoText = ammo.gameObject.AddComponent<Text>();
        ammoText.font = UIKit.DefaultFont;
        ammoText.fontSize = 30;
        ammoText.alignment = TextAnchor.MiddleRight;
        ammoText.color = new Color(0.95f, 0.85f, 0.45f);
        ammoText.raycastTarget = false;
        ammoText.text = "";
    }

    void BuildDeathPanel()
    {
        UIKit.EnsureEventSystem();
        var canvas = UIKit.CreateCanvas("MuertePantalla", 50);
        canvas.transform.SetParent(transform, false);
        deathPanel = canvas.gameObject;

        UIKit.Panel(canvas.transform, "Fondo", new Color(0.12f, 0f, 0f, 0.85f));
        UIKit.Label(canvas.transform, "HAS MUERTO", 72, new Vector2(0f, 120f), new Vector2(900f, 100f), TextAnchor.MiddleCenter, new Color(0.85f, 0.1f, 0.1f));
        UIKit.Button(canvas.transform, "Reiniciar", new Vector2(0f, 0f), new Vector2(300f, 56f), () =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        });
        UIKit.Button(canvas.transform, "Menu principal", new Vector2(0f, -70f), new Vector2(300f, 56f), () =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(MainMenu.MenuSceneName);
        });
        deathPanel.SetActive(false);
    }

    static RectTransform MakeRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    void Refresh()
    {
        float ratio = maxHealth > 0f ? Mathf.Clamp01(CurrentHealth / maxHealth) : 0f;
        if (fill != null) fill.anchorMax = new Vector2(ratio, 1f);
        if (healthText != null) healthText.text = "VIDA  " + Mathf.CeilToInt(CurrentHealth) + " / " + Mathf.CeilToInt(maxHealth);
    }

    void Update()
    {
        // el destello se desvanece
        if (flashAlpha > 0f)
        {
            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.unscaledDeltaTime * 0.9f);
            if (flash != null) flash.color = new Color(0.7f, 0f, 0f, flashAlpha);
        }

        // con poca vida la barra parpadea
        if (fillImage != null)
        {
            bool low = !IsDead && CurrentHealth <= maxHealth * 0.25f;
            float k = low ? 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)) : 1f;
            fillImage.color = new Color(0.75f * k, 0.1f * k, 0.1f * k);
        }

        UpdateAmmo();
    }

    void UpdateAmmo()
    {
        if (ammoText == null) return;
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + 0.5f;
            if (weapon == null) weapon = FindFirstObjectByType<PistolWeapon>();
        }

        if (weapon == null) { ammoText.text = ""; return; }

        int reserve = player != null ? Inventory.Of(player).reserveAmmo : 0;
        ammoText.text = weapon.IsReloading
            ? "RECARGANDO…"
            : "BALAS  " + weapon.AmmoInMagazine + " / " + reserve;
    }
}
