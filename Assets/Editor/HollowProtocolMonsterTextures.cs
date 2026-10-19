using UnityEditor;
using UnityEngine;

// Tools > Hollow Protocol > Texturizar monstruo
// Crea el material URP del "Smily horror monster" con sus texturas y se lo asigna al FBX.
public static class HollowProtocolMonsterTextures
{
    const string Fbx = "Assets/_Models/shia lebeuofsf.fbx";
    const string Dir = "Assets/_Textures/Monstruo/";
    const string MatPath = "Assets/_Materials/Monstruo.mat";

    [MenuItem("Tools/Hollow Protocol/Texturizar monstruo")]
    public static void Apply()
    {
        // el mapa normal debe importarse como Normal Map
        SetType(Dir + "Material.001_Normal_OpenGL.png", TextureImporterType.NormalMap, false);
        SetType(Dir + "Material.001_Base_Color.png", TextureImporterType.Default, true);
        SetType(Dir + "Material.001_Mixed_AO.png", TextureImporterType.Default, false);

        var baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "Material.001_Base_Color.png");
        var normal  = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "Material.001_Normal_OpenGL.png");
        var ao      = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "Material.001_Mixed_AO.png");
        if (baseMap == null) { EditorUtility.DisplayDialog("Hollow Protocol", "No encontré las texturas en " + Dir, "OK"); return; }

        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");

        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null) { mat = new Material(sh); AssetDatabase.CreateAsset(mat, MatPath); }
        mat.shader = sh;

        mat.SetTexture("_BaseMap", baseMap);
        mat.SetTexture("_MainTex", baseMap);
        mat.SetColor("_BaseColor", Color.white);
        if (normal != null) { mat.SetTexture("_BumpMap", normal); mat.EnableKeyword("_NORMALMAP"); }
        if (ao != null) { mat.SetTexture("_OcclusionMap", ao); mat.EnableKeyword("_OCCLUSIONMAP"); }
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.3f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        // asigna el material al FBX (reemplaza "Material.001")
        var imp = AssetImporter.GetAtPath(Fbx) as ModelImporter;
        if (imp == null) { EditorUtility.DisplayDialog("Hollow Protocol", "No encontré " + Fbx, "OK"); return; }
        imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Material.001"), mat);
        imp.SaveAndReimport();

        Debug.Log("[Hollow Protocol] Monstruo texturizado con " + MatPath);
    }

    static void SetType(string path, TextureImporterType type, bool sRGB)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;
        ti.textureType = type;
        if (type != TextureImporterType.NormalMap) ti.sRGBTexture = sRGB;
        ti.SaveAndReimport();
    }
}
