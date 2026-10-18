using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public static class AssignTextures
{
    const string MaterialsFolder = "Assets/_Materials";
    const string TexturesFolder = "Assets/_Textures";

    // Materiales que deben brillar (usan el mapa Emission)
    static readonly string[] EmissiveMats = { "Emergencia", "LuzFria", "Pantalla", "Liquido" };
    // Materiales transparentes
    static readonly string[] TransparentMats = { "Vidrio", "Liquido" };

    [MenuItem("Tools/Hollow Protocol/Asignar texturas")]
    public static void Run()
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("No se encontro el shader URP Lit. Revisa que URP este instalado y activo.");
            return;
        }

        // Indexar texturas por nombre
        var texGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { TexturesFolder });
        var textures = new Dictionary<string, string>(); // nombre asset -> ruta
        foreach (var g in texGuids)
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            textures[System.IO.Path.GetFileNameWithoutExtension(p)] = p;
        }

        var matGuids = AssetDatabase.FindAssets("t:Material", new[] { MaterialsFolder });
        int count = 0;

        foreach (var g in matGuids)
        {
            var matPath = AssetDatabase.GUIDToAssetPath(g);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) continue;

            string key = mat.name;
            if (mat.shader != urpLit) mat.shader = urpLit;

            var albedo = Find(textures, key, "AlbedoTransparency");
            var metallic = Find(textures, key, "MetallicSmoothness");
            var normal = Find(textures, key, "Normal");
            var emission = Find(textures, key, "Emission");

            if (albedo != null)
            {
                mat.SetTexture("_BaseMap", Load(albedo));
                mat.SetColor("_BaseColor", Color.white);
            }

            if (metallic != null)
            {
                SetLinear(metallic);
                mat.SetTexture("_MetallicGlossMap", Load(metallic));
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                mat.SetFloat("_SmoothnessTextureChannel", 0f); // Metallic alpha
            }

            if (normal != null)
            {
                SetNormalMap(normal);
                mat.SetTexture("_BumpMap", Load(normal));
                mat.SetFloat("_BumpScale", 1f);
                mat.EnableKeyword("_NORMALMAP");
            }

            bool isEmissive = EmissiveMats.Any(e => key.Contains(e));
            if (isEmissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                mat.SetColor("_EmissionColor", Color.white);
                if (emission != null) mat.SetTexture("_EmissionMap", Load(emission));
                else mat.SetColor("_EmissionColor", mat.GetColor("_BaseColor") * 1.5f);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
            }

            if (TransparentMats.Any(t => key.Contains(t)))
                MakeTransparent(mat);

            EditorUtility.SetDirty(mat);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Texturas asignadas a " + count + " materiales.");
    }

    // Busca la textura cuyo nombre contenga "<material>_<tipo>"
    static string Find(Dictionary<string, string> textures, string matName, string type)
    {
        string needle = matName + "_" + type;
        foreach (var kv in textures)
            if (kv.Key.Contains(needle)) return kv.Value;
        return null;
    }

    static Texture2D Load(string path) => AssetDatabase.LoadAssetAtPath<Texture2D>(path);

    static void SetNormalMap(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null && imp.textureType != TextureImporterType.NormalMap)
        {
            imp.textureType = TextureImporterType.NormalMap;
            imp.SaveAndReimport();
        }
    }

    static void SetLinear(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null && imp.sRGBTexture)
        {
            imp.sRGBTexture = false;
            imp.SaveAndReimport();
        }
    }

    static void MakeTransparent(Material mat)
    {
        mat.SetFloat("_Surface", 1f);   // Transparent
        mat.SetFloat("_Blend", 0f);     // Alpha
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        var c = mat.GetColor("_BaseColor");
        c.a = 0.4f;
        mat.SetColor("_BaseColor", c);
    }
}