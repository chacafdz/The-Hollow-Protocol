using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Menu: Tools > Hollow Protocol > Crear cielo nocturno
// Genera un cielo con estrellas y luna, crea el material, y ajusta ambiente, niebla y luz de luna.
public static class NightSkyTool
{
    const string Folder = "Assets/_Sky";
    const int W = 4096, H = 2048;

    [MenuItem("Tools/Hollow Protocol/Crear cielo nocturno")]
    public static void Create()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "_Sky");

        var px = new Color32[W * H];

        // Gradiente: horizonte azulado, cenit casi negro, suelo oscuro
        Color horizon = new Color(0.045f, 0.075f, 0.14f);
        Color zenith = new Color(0.004f, 0.008f, 0.025f);
        Color ground = new Color(0.004f, 0.006f, 0.010f);
        for (int y = 0; y < H; y++)
        {
            float lat = ((y / (float)(H - 1)) - 0.5f) * 180f;
            Color c = lat < 0f
                ? Color.Lerp(horizon * 0.5f, ground, Mathf.Clamp01(-lat / 20f))
                : Color.Lerp(horizon, zenith, Mathf.Pow(Mathf.Clamp01(lat / 70f), 0.6f));
            Color32 c32 = c;
            for (int x = 0; x < W; x++) px[y * W + x] = c32;
        }

        // Estrellas (se estiran segun la latitud para no deformarse en el cielo)
        var rnd = new System.Random(7);
        for (int i = 0; i < 4200; i++)
        {
            float lat = Mathf.Asin((float)(0.03 + rnd.NextDouble() * 0.97));
            int cx = rnd.Next(W);
            int cy = (int)((lat / Mathf.PI + 0.5f) * (H - 1));
            float mag = Mathf.Pow((float)rnd.NextDouble(), 4f);
            float bright = 0.25f + mag * 0.9f;
            float radius = 0.7f + mag * 1.6f;
            Color sc = Color.Lerp(new Color(0.75f, 0.85f, 1f), new Color(1f, 0.92f, 0.75f), (float)rnd.NextDouble());
            float stretch = 1f / Mathf.Max(0.2f, Mathf.Cos(lat));
            int rx = Mathf.Min(40, Mathf.CeilToInt(radius * stretch * 2f));
            int ry = Mathf.CeilToInt(radius * 2f);
            for (int dy = -ry; dy <= ry; dy++)
                for (int dx = -rx; dx <= rx; dx++)
                {
                    float d2 = (dx / stretch) * (dx / stretch) + dy * dy;
                    float k = Mathf.Exp(-d2 / (radius * radius)) * bright;
                    if (k < 0.01f) continue;
                    Add(px, cx + dx, cy + dy, sc.r * k, sc.g * k, sc.b * k);
                }
        }

        // Luna
        float moonLat = 35f * Mathf.Deg2Rad;
        float moonU = 0.30f;
        float moonV = moonLat / Mathf.PI + 0.5f;
        int mx = (int)(moonU * W);
        int my = (int)(moonV * (H - 1));
        float R = 26f;
        float cosL = Mathf.Cos(moonLat);
        int by = (int)(R * 5f);
        int bx = (int)(R * 5f / cosL);
        for (int dy = -by; dy <= by; dy++)
            for (int dx = -bx; dx <= bx; dx++)
            {
                float dist = Mathf.Sqrt((dx * cosL) * (dx * cosL) + dy * dy);
                if (dist < R)
                {
                    float n = Mathf.PerlinNoise(dx * 0.12f + 50f, dy * 0.12f + 50f);
                    float shade = 0.72f + 0.28f * Mathf.Clamp01(n * 1.4f);
                    Color mc = new Color(0.93f, 0.94f, 0.86f) * shade;
                    SetPixel(px, mx + dx, my + dy, mc);
                }
                else
                {
                    float glow = Mathf.Exp(-(dist - R) / (R * 1.4f)) * 0.30f;
                    Add(px, mx + dx, my + dy, 0.6f * glow, 0.7f * glow, 1f * glow);
                }
            }

        // Guardar textura
        string texPath = Folder + "/NightSky.png";
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        File.WriteAllBytes(texPath, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(texPath);

        var imp = (TextureImporter)AssetImporter.GetAtPath(texPath);
        imp.maxTextureSize = 4096;
        imp.mipmapEnabled = false;
        imp.wrapModeU = TextureWrapMode.Repeat;
        imp.wrapModeV = TextureWrapMode.Clamp;
        imp.textureCompression = TextureImporterCompression.CompressedHQ;
        imp.SaveAndReimport();

        // Material del cielo
        string matPath = Folder + "/NightSky.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            var sh = Shader.Find("Skybox/Panoramic");
            if (sh == null) { Debug.LogError("No se encontro el shader Skybox/Panoramic."); return; }
            mat = new Material(sh);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        mat.SetFloat("_Exposure", 1f);
        mat.SetFloat("_Rotation", 0f);
        EditorUtility.SetDirty(mat);

        // Ambiente, niebla y luz de luna
        RenderSettings.skybox = mat;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.02f, 0.03f, 0.06f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.03f, 0.05f, 0.09f);
        RenderSettings.fogDensity = 0.03f;

        // Direccion de la luna segun el mapeo del shader Skybox/Panoramic
        float lon = (0.5f - moonU) * 2f * Mathf.PI;
        float theta = (1f - moonV) * Mathf.PI;
        Vector3 moonDir = new Vector3(Mathf.Sin(theta) * Mathf.Cos(lon), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(lon));

        Light sun = RenderSettings.sun;
        if (sun == null)
            foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sun = l; break; }

        if (sun != null)
        {
            sun.color = new Color(0.55f, 0.65f, 1f);
            sun.intensity = 0.35f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.LookRotation(-moonDir);
            RenderSettings.sun = sun;
        }
        else Debug.LogWarning("No hay Directional Light en la escena; crea una y vuelve a ejecutar la herramienta.");

        DynamicGI.UpdateEnvironment();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Cielo nocturno creado en " + Folder + " y aplicado a la escena.");
    }

    static void Add(Color32[] px, int x, int y, float r, float g, float b)
    {
        if (y < 0 || y >= H) return;
        x = ((x % W) + W) % W;
        Color32 p = px[y * W + x];
        px[y * W + x] = new Color32(
            (byte)Mathf.Min(255f, p.r + r * 255f),
            (byte)Mathf.Min(255f, p.g + g * 255f),
            (byte)Mathf.Min(255f, p.b + b * 255f), 255);
    }

    static void SetPixel(Color32[] px, int x, int y, Color c)
    {
        if (y < 0 || y >= H) return;
        x = ((x % W) + W) % W;
        px[y * W + x] = c;
    }
}
