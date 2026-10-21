using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Crea el prefab del basurero (modelado en Maya) con material metalico y collider,
/// y coloca una copia en la escena junto al laboratorio.
/// </summary>
public static class HollowProtocolProps
{
    const string ModelPath = "Assets/_Prefabs/Basurero.fbx";
    const string PrefabPath = "Assets/_Prefabs/Basurero.prefab";
    const string MatPath = "Assets/_Materials/Basurero_Metal.mat";

    [MenuItem("Tools/Hollow Protocol/Crear prefab del basurero y ponerlo en la escena")]
    public static void Create()
    {
        AssetDatabase.ImportAsset(ModelPath);
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null) { Debug.LogError("[Hollow Protocol] No encuentro " + ModelPath); return; }

        // material metalico (galvanizado) en URP
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Materials")) AssetDatabase.CreateFolder("Assets", "_Materials");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, MatPath);
        }
        mat.shader = shader;
        mat.SetColor("_BaseColor", new Color(0.62f, 0.64f, 0.66f));
        mat.SetFloat("_Metallic", 0.9f);
        mat.SetFloat("_Smoothness", 0.55f);
        EditorUtility.SetDirty(mat);

        // instancia temporal -> material, collider -> prefab
        var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
        go.name = "Basurero";
        Bounds b = new Bounds(go.transform.position, Vector3.zero);
        bool has = false;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
            if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
        }
        var box = go.AddComponent<BoxCollider>();
        box.center = go.transform.InverseTransformPoint(b.center);
        box.size = b.size;

        var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(go, PrefabPath, InteractionMode.UserAction);

        // lo coloca cerca del laboratorio, apoyado en el suelo
        Vector3 pos = Vector3.zero;
        var lab = GameObject.Find("labvV2Inicio") ?? GameObject.Find("labV2");
        if (lab != null) pos = lab.transform.position + new Vector3(4f, 0f, -4f);
        if (Physics.Raycast(pos + Vector3.up * 50f, Vector3.down, out var hit, 200f)) pos = hit.point;
        else if (Terrain.activeTerrain != null) pos.y = Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.transform.position.y;
        go.transform.position = pos;

        Undo.RegisterCreatedObjectUndo(go, "Basurero");
        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[Hollow Protocol] Prefab " + PrefabPath + " creado (altura " + b.size.y.ToString("0.00") +
                  " m). Quedo seleccionado en la escena: muevelo a donde quieras y guarda con Ctrl+S.");
    }
}
