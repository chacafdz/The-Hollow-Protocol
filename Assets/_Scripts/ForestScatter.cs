using System.Collections.Generic;
using UnityEngine;

// Reparte prefabs de arboles por el terreno sin usar la herramienta de arboles del Terrain.
// Uso: poner en un objeto vacio, asignar prefabs, y clic derecho en el componente > "Generar bosque".
// El area es un rectangulo centrado en este objeto, recortado para no salirse del Terrain.
public class ForestScatter : MonoBehaviour
{
    [Header("Arboles (prefabs de Realistic Tree > Prefabs > URP)")]
    public GameObject[] treePrefabs;

    [Header("Area (centrada en este objeto)")]
    [Tooltip("Desmarcado = el bosque cubre todo el Terrain. Marcado = usa Area Size alrededor de este objeto.")]
    public bool useManualArea = false;
    public Vector2 areaSize = new Vector2(300f, 300f);
    public bool clampToTerrain = true;           // recorta el area a los limites del Terrain
    public float terrainMargin = 5f;             // margen interior respecto al borde del Terrain
    public int treeCount = 400;                  // maximo 3000 por seguridad
    public float minSpacing = 4f;
    public int seed = 7;

    [Header("Zonas libres (laboratorio, cabana, camino)")]
    public Transform[] clearings;                // si tienen mallas, se usa su caja real
    public float clearingMargin = 6f;            // margen extra alrededor de esa caja
    public float clearingRadius = 14f;           // solo para objetos vacios sin mallas
    [Tooltip("Busca estos objetos por nombre en la escena y deja libre su zona automaticamente")]
    public string[] autoClearNames = { "torre", "casavieja", "cabana", "cabanachida", "labv2", "labvv2inicio" };

    [Header("Caminos (splines)")]
    [Tooltip("Marca esto para que los arboles ignoren los caminos hechos con Spline Extrude")]
    public bool ignoreSplinePaths = false;
    [Tooltip("Distancia libre a cada lado del borde del camino, en metros (0 = 4 m)")]
    public float pathMargin = 4f;

    [Header("Variacion")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.3f);

    [Header("Borde denso (pared de arboles para no salirse del mapa)")]
    public bool denseBorder = true;
    public float borderWidth = 12f;
    public int borderExtraTrees = 80;

    const string ContainerName = "Bosque_Generado";
    const int HardLimit = 5000;

    readonly List<Rect> clearRects = new List<Rect>();
    readonly List<Vector3> clearCircles = new List<Vector3>();   // x, z, radio
    readonly List<GameObject> valid = new List<GameObject>();

    [ContextMenu("Generar bosque")]
    public void Generate()
    {
        // Solo prefabs con malla real: los efectos de particulas (hojas cayendo) se omiten
        valid.Clear();
        if (treePrefabs != null)
            foreach (var p in treePrefabs)
            {
                if (p == null) continue;
                bool hasMesh = p.GetComponentInChildren<MeshRenderer>(true) != null
                            || p.GetComponentInChildren<SkinnedMeshRenderer>(true) != null;
                if (hasMesh) valid.Add(p);
                else Debug.LogWarning("ForestScatter: se omite '" + p.name + "' porque no tiene malla (parece un efecto de particulas).");
            }

        if (valid.Count == 0)
        {
            Debug.LogWarning("ForestScatter: no hay prefabs de arbol validos.");
            return;
        }

        Rect area = GetArea();
        if (area.width <= 0f || area.height <= 0f)
        {
            Debug.LogWarning("ForestScatter: el area queda fuera del Terrain. Mueve este objeto sobre el terreno.");
            return;
        }

        int target = Mathf.Min(treeCount, HardLimit);
        int extraTarget = Mathf.Min(borderExtraTrees, HardLimit);
        if (treeCount > HardLimit)
            Debug.LogWarning("ForestScatter: Tree Count limitado a " + HardLimit + " para no bloquear el editor.");

        Clear();
        BuildClearings();

        var container = new GameObject(ContainerName).transform;
        container.SetParent(transform, false);

        Random.InitState(seed);
        var placed = new List<Vector3>();

        // Arboles repartidos por el area
        for (int i = 0; i < target * 25 && placed.Count < target; i++)
        {
            Vector3 p = new Vector3(Random.Range(area.xMin, area.xMax), 0f, Random.Range(area.yMin, area.yMax));
            TryPlace(p, minSpacing, container, placed);
        }

        // Borde mas denso
        if (denseBorder)
        {
            int extra = 0;
            for (int i = 0; i < extraTarget * 25 && extra < extraTarget; i++)
            {
                Vector3 p = new Vector3(Random.Range(area.xMin, area.xMax), 0f, Random.Range(area.yMin, area.yMax));
                float distEdge = Mathf.Min(
                    Mathf.Min(p.x - area.xMin, area.xMax - p.x),
                    Mathf.Min(p.z - area.yMin, area.yMax - p.z));
                if (distEdge > borderWidth) continue;
                if (TryPlace(p, minSpacing * 0.7f, container, placed)) extra++;
            }
        }

        Debug.Log("ForestScatter: " + placed.Count + " arboles generados.");

#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    [ContextMenu("Borrar bosque")]
    public void Clear()
    {
        var old = transform.Find(ContainerName);
        if (old != null) DestroyImmediate(old.gameObject);
    }

    // Rectangulo en XZ: area pedida, recortada al Terrain
    Rect GetArea()
    {
        // Por defecto cubre TODO el Terrain (ignora la posicion y el tamano de este objeto)
        var full = Terrain.activeTerrain;
        if (!useManualArea && full != null)
        {
            Vector3 fp = full.transform.position;
            Vector3 fs = full.terrainData.size;
            return Rect.MinMaxRect(fp.x + terrainMargin, fp.z + terrainMargin,
                                   fp.x + fs.x - terrainMargin, fp.z + fs.z - terrainMargin);
        }

        Vector3 c = transform.position;
        float xMin = c.x - areaSize.x / 2f, xMax = c.x + areaSize.x / 2f;
        float zMin = c.z - areaSize.y / 2f, zMax = c.z + areaSize.y / 2f;

        var t = Terrain.activeTerrain;
        if (clampToTerrain && t != null)
        {
            Vector3 tp = t.transform.position;
            Vector3 ts = t.terrainData.size;
            xMin = Mathf.Max(xMin, tp.x + terrainMargin);
            xMax = Mathf.Min(xMax, tp.x + ts.x - terrainMargin);
            zMin = Mathf.Max(zMin, tp.z + terrainMargin);
            zMax = Mathf.Min(zMax, tp.z + ts.z - terrainMargin);
        }

        return Rect.MinMaxRect(xMin, zMin, xMax, zMax);
    }

    // Calcula una sola vez las zonas libres
    void BuildClearings()
    {
        clearRects.Clear();
        clearCircles.Clear();
        BuildPathGrid();

        // zonas libres automaticas por nombre (torre, casa, cabana, laboratorio)
        var toClear = new List<Transform>();
        if (clearings != null) toClear.AddRange(clearings);
        // Unity guarda una lista vacia en escenas viejas: si pasa, se usan los nombres por defecto
        var names = (autoClearNames != null && autoClearNames.Length > 0)
            ? autoClearNames
            : new[] { "torre", "casavieja", "cabana", "cabanachida", "labv2", "labvv2inicio" };
        if (names.Length > 0)
        {
            var container = transform.Find(ContainerName);
            foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (container != null && t.IsChildOf(container)) continue;
                string n = t.name.ToLowerInvariant().Replace("ñ", "n").Replace(" ", "").Replace("_", "");
                foreach (var want in names)
                    if (n == want) { toClear.Add(t); break; }
            }
        }

        foreach (var z in toClear)
        {
            if (z == null) continue;

            var rends = z.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                foreach (var r in rends) b.Encapsulate(r.bounds);
                clearRects.Add(Rect.MinMaxRect(
                    b.min.x - clearingMargin, b.min.z - clearingMargin,
                    b.max.x + clearingMargin, b.max.z + clearingMargin));
            }
            else
            {
                clearCircles.Add(new Vector3(z.position.x, z.position.z, clearingRadius));
            }
        }
    }

    // ---- caminos: puntos de la malla extruida del spline, en una rejilla para buscar rapido
    readonly Dictionary<Vector2Int, List<Vector2>> pathGrid = new Dictionary<Vector2Int, List<Vector2>>();
    float pathCell = 6f;
    float pathClear = 4f;

    static bool IsSplinePath(Transform t)
    {
        if (t.GetComponent("SplineExtrude") != null) return true;                 // componente del paquete Splines
        return t.name.StartsWith("Spline") && t.GetComponent<MeshFilter>() != null;
    }

    void BuildPathGrid()
    {
        pathGrid.Clear();
        if (ignoreSplinePaths) return;
        pathClear = pathMargin > 0f ? pathMargin : 4f;
        pathCell = Mathf.Max(6f, pathClear);

        int roads = 0;
        foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (!IsSplinePath(t)) continue;
            var mf = t.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            var verts = mf.sharedMesh.vertices;
            foreach (var v in verts)
            {
                Vector3 w = t.TransformPoint(v);
                var key = new Vector2Int(Mathf.FloorToInt(w.x / pathCell), Mathf.FloorToInt(w.z / pathCell));
                if (!pathGrid.TryGetValue(key, out var list)) { list = new List<Vector2>(); pathGrid[key] = list; }
                list.Add(new Vector2(w.x, w.z));
            }
            roads++;
        }
        if (roads > 0) Debug.Log("ForestScatter: " + roads + " camino(s) detectados, margen " + pathClear + " m.");
    }

    bool NearPath(Vector2 pt)
    {
        if (pathGrid.Count == 0) return false;
        int cx = Mathf.FloorToInt(pt.x / pathCell), cy = Mathf.FloorToInt(pt.y / pathCell);
        float r2 = pathClear * pathClear;
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (pathGrid.TryGetValue(new Vector2Int(cx + dx, cy + dy), out var list))
                    foreach (var q in list)
                        if ((q - pt).sqrMagnitude < r2) return true;
        return false;
    }

    bool InClearing(Vector3 p)
    {
        var pt = new Vector2(p.x, p.z);
        if (NearPath(pt)) return true;
        foreach (var r in clearRects)
            if (r.Contains(pt)) return true;
        foreach (var c in clearCircles)
            if ((pt - new Vector2(c.x, c.y)).sqrMagnitude < c.z * c.z) return true;
        return false;
    }

    bool TryPlace(Vector3 p, float spacing, Transform parent, List<Vector3> placed)
    {
        if (InClearing(p)) return false;

        foreach (var q in placed)
        {
            Vector2 d = new Vector2(p.x - q.x, p.z - q.z);
            if (d.sqrMagnitude < spacing * spacing) return false;
        }

        var t = Terrain.activeTerrain;
        p.y = t != null ? t.SampleHeight(p) + t.transform.position.y : transform.position.y;

        var prefab = valid[Random.Range(0, valid.Count)];
        if (prefab == null) return false;

        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        GameObject go;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // Instancia ligada al prefab: la escena guarda solo una referencia (~1 KB por arbol)
            // en vez de copiar todo el arbol (~100 KB), que es lo que hacia pesar la escena 565 MB.
            go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.SetPositionAndRotation(p, rot);
        }
        else
#endif
        {
            go = Instantiate(prefab, p, rot, parent);
        }
        go.transform.localScale *= Random.Range(scaleRange.x, scaleRange.y);
        placed.Add(p);
        return true;
    }
}
