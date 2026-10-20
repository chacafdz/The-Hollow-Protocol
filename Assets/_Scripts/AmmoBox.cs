using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Crea la caja con 6 balas arriba de la cama de la torre. Se coloca sola al cargar el nivel
/// (si no hay ya una en la escena) y tambien se puede dejar fija con el menu del editor
/// "Crear caja de municion en la torre".
/// </summary>
public static class AmmoBox
{
    /// <summary>Nombre del objeto de la caja.</summary>
    public const string BoxName = "Caja_Municion";

    /// <summary>Balas que contiene.</summary>
    public const int Bullets = 6;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
        Spawn();
    }

    static void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        Spawn();
    }

    /// <summary>Busca el colchon de la torre y pone la caja encima, si todavia no existe.</summary>
    public static void Spawn()
    {
        if (GameObject.Find(BoxName) != null) return;

        Transform mattress = null, pillow = null;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == "T_Cama_colchon") mattress = t;
            else if (t.name == "T_Cama_almohada") pillow = t;
        }
        if (mattress == null) return;      // menu o escena sin torre
        Create(mattress, pillow, false);
    }

    /// <summary>Construye la caja sobre el colchon, hacia los pies de la cama (lejos de la almohada).</summary>
    /// <param name="mattress">Colchon de la cama.</param>
    /// <param name="pillow">Almohada (opcional, indica hacia donde esta la cabecera).</param>
    /// <param name="persistentMaterials">En el editor guarda los materiales como archivos.</param>
    public static GameObject Create(Transform mattress, Transform pillow, bool persistentMaterials)
    {
        Bounds b = BoundsOf(mattress);
        Vector3 pos = new Vector3(b.center.x, b.max.y, b.center.z);
        Vector3 forward = Vector3.forward;
        if (pillow != null)
        {
            Vector3 d = b.center - BoundsOf(pillow).center;
            d.y = 0f;
            if (d.sqrMagnitude > 0.0001f) { forward = d.normalized; pos += forward * 0.25f; }
        }

        var root = new GameObject(BoxName);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, 15f, 0f);

        Part(root, "Base",     new Vector3(0f, 0.06f, 0f),    new Vector3(0.30f, 0.12f, 0.20f),  Mat("Municion_Caja",     new Color(0.24f, 0.27f, 0.11f), persistentMaterials));
        Part(root, "Tapa",     new Vector3(0f, 0.125f, 0f),   new Vector3(0.31f, 0.025f, 0.21f), Mat("Municion_Tapa",     new Color(0.16f, 0.19f, 0.07f), persistentMaterials));
        Part(root, "Etiqueta", new Vector3(0f, 0.1385f, 0f),  new Vector3(0.12f, 0.004f, 0.07f), Mat("Municion_Etiqueta", new Color(0.90f, 0.75f, 0.10f), persistentMaterials));

        var col = root.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.07f, 0f);
        col.size = new Vector3(0.32f, 0.15f, 0.22f);

        var pick = root.AddComponent<AmmoPickup>();
        pick.amount = Bullets;
        pick.prompt = "Pulsa E para recoger munición (+" + Bullets + ")";
        return root;
    }

    static Bounds BoundsOf(Transform t)
    {
        bool any = false; Bounds b = new Bounds(t.position, Vector3.one * 0.3f);
        foreach (var r in t.GetComponentsInChildren<Renderer>(true))
        {
            if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
        }
        return b;
    }

    static void Part(GameObject root, string name, Vector3 localPos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static Material Mat(string name, Color color, bool persistent)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");

#if UNITY_EDITOR
        if (persistent)
        {
            string path = "Assets/_Materials/" + name + ".mat";
            var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var asset = new Material(sh);
            asset.color = color;
            if (asset.HasProperty("_BaseColor")) asset.SetColor("_BaseColor", color);
            if (asset.HasProperty("_Smoothness")) asset.SetFloat("_Smoothness", 0.2f);
            UnityEditor.AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
#endif
        var m = new Material(sh);
        m.color = color;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.2f);
        return m;
    }
}
