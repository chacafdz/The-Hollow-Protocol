using UnityEditor;
using UnityEngine;

// Menu: Tools > Hollow Protocol > Limpiar pasto bajo la seleccion
// Selecciona en la jerarquia los edificios (labV2, labV2Inicio, torre, casa, cabana) y ejecuta el menu.
// Borra el pasto y los detalles del Terrain bajo cada uno, con un margen de 2 m.
public static class TerrainClearArea
{
    const float Margin = 2f;

    [MenuItem("Tools/Hollow Protocol/Limpiar pasto bajo la seleccion")]
    public static void Clear()
    {
        var terrain = Terrain.activeTerrain;
        if (terrain == null) { Debug.LogWarning("No hay Terrain activo en la escena."); return; }
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogWarning("Selecciona primero los edificios en la jerarquia.");
            return;
        }

        var td = terrain.terrainData;
        Undo.RegisterCompleteObjectUndo(td, "Limpiar pasto");

        int w = td.detailWidth, h = td.detailHeight;
        int layers = td.detailPrototypes.Length;
        Vector3 tp = terrain.transform.position, ts = td.size;
        int cleared = 0;

        foreach (var go in Selection.gameObjects)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) continue;

            Bounds b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);

            int x0 = Mathf.Clamp(Mathf.FloorToInt((b.min.x - Margin - tp.x) / ts.x * w), 0, w - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((b.max.x + Margin - tp.x) / ts.x * w), 0, w);
            int y0 = Mathf.Clamp(Mathf.FloorToInt((b.min.z - Margin - tp.z) / ts.z * h), 0, h - 1);
            int y1 = Mathf.Clamp(Mathf.CeilToInt((b.max.z + Margin - tp.z) / ts.z * h), 0, h);
            if (x1 - x0 <= 0 || y1 - y0 <= 0) continue;

            var zeros = new int[y1 - y0, x1 - x0];
            for (int l = 0; l < layers; l++) td.SetDetailLayer(x0, y0, l, zeros);
            cleared++;
        }

        EditorUtility.SetDirty(td);
        Debug.Log("Pasto eliminado bajo " + cleared + " objeto(s).");
    }
}
