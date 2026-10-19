using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

// Tools > Hollow Protocol > Arreglar arboles morados
// Recorre todos los prefabs de Assets/Realistic Tree. Cualquier material con shader roto o "Standard"
// (se ve magenta en URP) se cambia por el material URP del mismo nombre, o por una copia URP/Lit con
// la misma textura y recorte de alfa. Guarda los cambios en los prefabs.
public static class FixPurpleTrees
{
    const string Root = "Assets/Realistic Tree";
    const string FallbackDir = "Assets/_Materials/Arboles";

    [MenuItem("Tools/Hollow Protocol/Arreglar arboles morados")]
    public static void Fix()
    {
        var urpByName = new Dictionary<string, Material>();
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { Root }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.Contains("/URP/") || path.Contains("/Standard/")) continue;
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null && !IsBroken(m) && !urpByName.ContainsKey(m.name)) urpByName[m.name] = m;
        }

        var fallbacks = new Dictionary<Material, Material>();
        var particleCache = new Dictionary<Material, Material>();
        var report = new StringBuilder();
        int prefabsChanged = 0, matsReplaced = 0;

        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { Root }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var contents = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;

            foreach (var r in contents.GetComponentsInChildren<Renderer>(true))
            {
                // Hojas cayendo (partículas): siempre se reemplaza su material por uno URP válido
                if (r is ParticleSystemRenderer)
                {
                    var pm = r.sharedMaterials;
                    bool pc = false;
                    for (int i = 0; i < pm.Length; i++)
                    {
                        var np = GetParticleMat(pm[i], particleCache);
                        if (np != null && np != pm[i])
                        {
                            report.AppendLine("  " + System.IO.Path.GetFileName(path) + " (partículas): " + (pm[i] ? pm[i].name : "(null)") + " -> " + np.name);
                            pm[i] = np; pc = true; matsReplaced++;
                        }
                    }
                    if (pc) { r.sharedMaterials = pm; changed = true; }
                    continue;
                }

                var mats = r.sharedMaterials;
                bool rChanged = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (!IsBroken(m)) continue;

                    Material replacement = null;
                    if (m != null && urpByName.TryGetValue(m.name, out var u)) replacement = u;
                    else if (m != null) replacement = GetFallback(m, fallbacks);

                    if (replacement != null)
                    {
                        mats[i] = replacement;
                        rChanged = true;
                        matsReplaced++;
                        report.AppendLine("  " + System.IO.Path.GetFileName(path) + ": " + (m ? m.name : "(null)") + " -> " + replacement.name);
                    }
                }
                if (rChanged) { r.sharedMaterials = mats; changed = true; }
            }

            if (changed) { PrefabUtility.SaveAsPrefabAsset(contents, path); prefabsChanged++; }
            PrefabUtility.UnloadPrefabContents(contents);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string msg = "Prefabs modificados: " + prefabsChanged + "\nMateriales reemplazados: " + matsReplaced;
        Debug.Log("[Hollow Protocol] Arreglar arboles morados\n" + msg + "\n" + report);
        EditorUtility.DisplayDialog("Hollow Protocol", msg + "\n\nDetalle en la consola. Si sigues viendo morado, mira el log.", "OK");
    }

    // Tools > Hollow Protocol > Arreglar arboles (forzar URP Lit en todos los LOD)
    // Cambia TODOS los materiales de TODOS los renderers de los prefabs de arboles por copias URP/Lit
    // con la misma textura. Pierde el viento del shader original, pero se ve igual de cerca y de lejos.
    [MenuItem("Tools/Hollow Protocol/Arreglar arboles (forzar URP Lit en todos los LOD)")]
    public static void ForceLit()
    {
        var litCache = new Dictionary<Material, Material>();
        var partCache = new Dictionary<Material, Material>();
        var report = new StringBuilder();
        var errorShaders = new HashSet<string>();
        int prefabsChanged = 0, replaced = 0, lodGroups = 0;

        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { Root + "/Prefabs/URP" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var contents = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;
            lodGroups += contents.GetComponentsInChildren<LODGroup>(true).Length;

            foreach (var r in contents.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                bool rChanged = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var old = mats[i];
                    if (old == null) continue;
                    if (old.shader == null || !old.shader.isSupported) errorShaders.Add(old.shader != null ? old.shader.name : "(sin shader)");

                    Material nm = r is ParticleSystemRenderer ? GetParticleMat(old, partCache) : GetLit(old, litCache);
                    if (nm != null && nm != old) { mats[i] = nm; rChanged = true; replaced++; }
                }
                if (rChanged) { r.sharedMaterials = mats; changed = true; }
            }

            if (changed) { PrefabUtility.SaveAsPrefabAsset(contents, path); prefabsChanged++; }
            PrefabUtility.UnloadPrefabContents(contents);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string errs = errorShaders.Count == 0 ? "ninguno" : string.Join(", ", errorShaders);
        string msg = "Prefabs modificados: " + prefabsChanged + "\nMateriales reemplazados: " + replaced
                   + "\nLODGroups encontrados en los prefabs: " + lodGroups + "\nShaders con errores: " + errs;
        Debug.Log("[Hollow Protocol] Forzar URP Lit\n" + msg);
        EditorUtility.DisplayDialog("Hollow Protocol", msg + "\n\nAhora: Borrar bosque y Generar bosque.", "OK");
    }

    static Material GetLit(Material old, Dictionary<Material, Material> cache)
    {
        if (cache.TryGetValue(old, out var c)) return c;
        if (old.shader != null && old.shader.name == "Universal Render Pipeline/Lit") return old;

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) return null;

        if (!AssetDatabase.IsValidFolder("Assets/_Materials")) AssetDatabase.CreateFolder("Assets", "_Materials");
        if (!AssetDatabase.IsValidFolder(FallbackDir)) AssetDatabase.CreateFolder("Assets/_Materials", "Arboles");

        string path = FallbackDir + "/" + old.name + "_Lit.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(lit); AssetDatabase.CreateAsset(m, path); }
        m.shader = lit;

        Texture tex = FindTexture(old, false);
        Texture nrm = FindTexture(old, true);
        Color col = Color.white;
        foreach (var cn in new[] { "_BaseColor", "_Color", "_TintColor" })
            if (old.HasProperty(cn)) { col = old.GetColor(cn); break; }

        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", col);
        if (nrm != null) { m.SetTexture("_BumpMap", nrm); m.EnableKeyword("_NORMALMAP"); }
        m.SetFloat("_Smoothness", 0.1f);
        m.SetFloat("_Metallic", 0f);

        // corteza: opaca. hojas y agujas: recorte de alfa y doble cara
        string n = old.name.ToLowerInvariant();
        bool bark = n.Contains("bark") || n == "birch" || n == "chestnut" || n == "spruce" || n == "ash" || n == "white";
        if (!bark)
        {
            m.SetFloat("_AlphaClip", 1f);
            m.SetFloat("_Cutoff", 0.5f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cull", 0f);
            m.renderQueue = 2450;
        }
        EditorUtility.SetDirty(m);
        cache[old] = m;
        return m;
    }

    // Busca la textura principal (o el mapa normal) de un material, sea cual sea el nombre de su propiedad
    static Texture FindTexture(Material m, bool normal)
    {
        var names = m.GetTexturePropertyNames();
        foreach (var n in names)
        {
            string l = n.ToLowerInvariant();
            bool isNormal = l.Contains("normal") || l.Contains("bump");
            if (normal != isNormal) continue;
            if (!normal && (l.Contains("mask") || l.Contains("occlusion") || l.Contains("metal") || l.Contains("smooth") || l.Contains("emiss") || l.Contains("detail"))) continue;
            var t = m.GetTexture(n);
            if (t != null) return t;
        }
        return null;
    }

    static bool IsBroken(Material m)
    {
        if (m == null || m.shader == null) return true;
        string n = m.shader.name;
        return !m.shader.isSupported || n.StartsWith("Hidden/InternalError") || n == "Standard"
            || n.StartsWith("Legacy Shaders/") || n.StartsWith("Nature/");
    }

    // Material URP para hojas que caen: Particles/Unlit con la misma textura y recorte de alfa
    static Material GetParticleMat(Material old, Dictionary<Material, Material> cache)
    {
        if (old == null) return null;
        if (old.shader != null && old.shader.name == "Universal Render Pipeline/Particles/Unlit" && old.shader.isSupported) return old;
        if (cache.TryGetValue(old, out var c)) return c;

        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) return null;

        if (!AssetDatabase.IsValidFolder("Assets/_Materials")) AssetDatabase.CreateFolder("Assets", "_Materials");
        if (!AssetDatabase.IsValidFolder(FallbackDir)) AssetDatabase.CreateFolder("Assets/_Materials", "Arboles");

        string path = FallbackDir + "/" + old.name + "_Particulas.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
        m.shader = sh;

        Texture tex = old.HasProperty("_BaseMap") ? old.GetTexture("_BaseMap") : null;
        if (tex == null && old.HasProperty("_MainTex")) tex = old.GetTexture("_MainTex");
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Surface", 0f);          // opaco con recorte de alfa
        m.SetFloat("_AlphaClip", 1f);
        m.SetFloat("_Cutoff", 0.5f);
        m.SetFloat("_Cull", 0f);             // doble cara
        m.EnableKeyword("_ALPHATEST_ON");
        m.renderQueue = 2450;

        EditorUtility.SetDirty(m);
        cache[old] = m;
        return m;
    }

    static Material GetFallback(Material old, Dictionary<Material, Material> cache)
    {
        if (cache.TryGetValue(old, out var c)) return c;

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) return null;

        if (!AssetDatabase.IsValidFolder("Assets/_Materials")) AssetDatabase.CreateFolder("Assets", "_Materials");
        if (!AssetDatabase.IsValidFolder(FallbackDir)) AssetDatabase.CreateFolder("Assets/_Materials", "Arboles");

        string path = FallbackDir + "/" + old.name + "_URP.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(lit);
            AssetDatabase.CreateAsset(m, path);
        }

        Texture tex = old.HasProperty("_MainTex") ? old.GetTexture("_MainTex") : null;
        if (tex == null && old.HasProperty("_BaseMap")) tex = old.GetTexture("_BaseMap");
        Color col = old.HasProperty("_Color") ? old.GetColor("_Color") : Color.white;

        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", col);
        m.SetFloat("_Smoothness", 0.1f);
        m.SetFloat("_Metallic", 0f);

        // hojas: recorte de alfa y doble cara
        m.SetFloat("_AlphaClip", 1f);
        m.SetFloat("_Cutoff", 0.5f);
        m.EnableKeyword("_ALPHATEST_ON");
        m.SetFloat("_Cull", 0f);
        m.renderQueue = 2450;

        EditorUtility.SetDirty(m);
        cache[old] = m;
        return m;
    }
}
