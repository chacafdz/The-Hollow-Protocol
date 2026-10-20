using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Pone las imagenes de las pistas (mapa, foto, diario, nota) sobre sus objetos de la escena.
/// Los planos del FBX tienen UV que recortan o espejan la imagen, asi que en lugar de cambiar su material
/// se coloca encima un Quad propio (UV 0-1) en ambas caras, con la proporcion correcta de la imagen.
/// El objeto original queda con un material de papel liso.
/// </summary>
public static class HollowProtocolMapTexture
{
    const string QuadName = "ImagenPista";

    static readonly string[][] Items =
    {
        new[] { "Pista_Mapa",   "Mapa"   },
        new[] { "Pista_Foto",   "Foto"   },
        new[] { "Pista_Diario", "Diario" },
        new[] { "Pista_Nota",   "Nota"   },
        new[] { "Registro_Final", "Registro" },
    };

    [MenuItem("Tools/Hollow Protocol/Poner imagenes a las pistas (mapa, foto, diario, nota, registro)")]
    public static void ApplyAll()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) { Debug.LogError("[Hollow Protocol] No encuentro el shader URP/Lit."); return; }
        if (!AssetDatabase.IsValidFolder("Assets/_Materials")) AssetDatabase.CreateFolder("Assets", "_Materials");

        var paper = GetMat("Assets/_Materials/Pista_Papel.mat", shader);
        paper.SetTexture("_BaseMap", null);
        paper.SetColor("_BaseColor", new Color(0.72f, 0.65f, 0.52f));
        paper.SetFloat("_Smoothness", 0f);
        paper.DisableKeyword("_EMISSION");
        EditorUtility.SetDirty(paper);

        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var report = new System.Text.StringBuilder("[Hollow Protocol] Imagenes de pistas:\n");

        foreach (var it in Items)
        {
            string objName = it[0], img = it[1];
            string texPath = "Assets/_Textures/Mapa/" + img + ".png";
            AssetDatabase.ImportAsset(texPath);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex == null) { report.AppendLine("FALTA  imagen " + texPath); continue; }

            var mat = GetMat("Assets/_Materials/Pista_" + img + ".mat", shader);
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0f);
            mat.SetFloat("_Metallic", 0f);
            mat.EnableKeyword("_EMISSION");
            mat.SetTexture("_EmissionMap", tex);
            mat.SetColor("_EmissionColor", new Color(0.15f, 0.15f, 0.15f));   // se lee de noche sin quemarse
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(mat);

            Transform target = null;
            foreach (var t in all) if (t != null && t.name == objName) { target = t; break; }
            if (target == null) { report.AppendLine("FALTA  objeto " + objName + " en la escena"); continue; }

            // borra quads de corridas anteriores
            var old = new List<GameObject>();
            foreach (var t in target.GetComponentsInChildren<Transform>(true)) if (t.name == QuadName) old.Add(t.gameObject);
            foreach (var g in old) Undo.DestroyObjectImmediate(g);

            // el renderer mas grande es la hoja; los que tenian una imagen vieja vuelven a papel
            MeshRenderer sheet = null; float best = 0f;
            foreach (var r in target.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].name.StartsWith("Pista_")) { mats[i] = paper; changed = true; }
                if (changed) { Undo.RecordObject(r, "Imagen de pista"); r.sharedMaterials = mats; }

                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                float area = r.bounds.size.x * r.bounds.size.z + r.bounds.size.x * r.bounds.size.y + r.bounds.size.y * r.bounds.size.z;
                if (area > best) { best = area; sheet = r; }
            }
            if (sheet == null) { report.AppendLine("FALTA  " + objName + " no tiene malla"); continue; }

            // ---- Documento estilo Resident Evil: imagen a pantalla completa + transcripcion
            var clue = target.GetComponentInParent<ClueNote>();
            if (clue == null) clue = target.GetComponentInChildren<ClueNote>(true);
            if (clue != null)
            {
                Undo.RecordObject(clue, "Documento de pista");
                clue.documentImage = tex;
                clue.documentTitle = Title(img);
                clue.transcript = Transcript(img);
                clue.clueText = Transcript(img);
                EditorUtility.SetDirty(clue);
                report.AppendLine("OK     " + objName + " -> se abre como documento (" + clue.documentTitle + ")");
            }
            else if (img != "Registro") report.AppendLine("FALTA  " + objName + " no tiene ClueNote (corre Conectar interactuables)");

            float aspect = tex.width / (float)tex.height;
            AddQuads(sheet, mat, aspect);
            report.AppendLine("OK     " + objName + " -> " + img + " (sobre " + sheet.name + ")");
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        report.AppendLine("Guarda la escena (Ctrl+S). Si una imagen sale de cabeza, gira 180 en Z el objeto ImagenPista.");
        Debug.Log(report.ToString());
    }

    static string Title(string img) { return DocumentLibrary.Title(img); }

    static string Transcript(string img) { return DocumentLibrary.Transcript(img); }

    static Material GetMat(string path, Shader shader)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        m.shader = shader;
        return m;
    }

    // Coloca un Quad en cada cara de la hoja, centrado y con la proporcion de la imagen.
    static void AddQuads(MeshRenderer r, Material mat, float aspect)
    {
        Bounds b = r.GetComponent<MeshFilter>().sharedMesh.bounds;
        Transform t = r.transform;
        Vector3 c = t.TransformPoint(b.center);
        Vector3[] ax =
        {
            t.TransformVector(new Vector3(b.size.x, 0f, 0f)),
            t.TransformVector(new Vector3(0f, b.size.y, 0f)),
            t.TransformVector(new Vector3(0f, 0f, b.size.z)),
        };

        int n = 0;
        for (int i = 1; i < 3; i++) if (ax[i].magnitude < ax[n].magnitude) n = i;
        int i1 = (n + 1) % 3, i2 = (n + 2) % 3;
        Vector3 normal = ax[n].sqrMagnitude > 1e-10f ? ax[n].normalized : Vector3.Cross(ax[i1], ax[i2]).normalized;
        float thick = ax[n].magnitude;

        Vector3 upAx, rightAx;
        if (Mathf.Abs(normal.y) < 0.7f)
        {
            // pared o cuadro: "arriba" es el eje del plano mas alineado con el mundo
            bool firstUp = Mathf.Abs(Vector3.Dot(ax[i1].normalized, Vector3.up)) >= Mathf.Abs(Vector3.Dot(ax[i2].normalized, Vector3.up));
            upAx = firstUp ? ax[i1] : ax[i2];
            rightAx = firstUp ? ax[i2] : ax[i1];
            if (Vector3.Dot(upAx, Vector3.up) < 0f) upAx = -upAx;
        }
        else
        {
            // sobre una mesa: imagen vertical -> arriba en el eje largo; horizontal -> en el corto
            bool firstLong = ax[i1].magnitude >= ax[i2].magnitude;
            Vector3 lng = firstLong ? ax[i1] : ax[i2], sht = firstLong ? ax[i2] : ax[i1];
            upAx = aspect < 1f ? lng : sht;
            rightAx = aspect < 1f ? sht : lng;
        }

        float upLen = upAx.magnitude, rightLen = rightAx.magnitude;
        float h = upLen * 0.94f, w = h * aspect;
        if (w > rightLen * 0.94f) { w = rightLen * 0.94f; h = w / aspect; }
        Vector3 upDir = upAx.normalized;

        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 outN = normal * s;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = QuadName;
            Object.DestroyImmediate(q.GetComponent<Collider>());   // no tapa el raycast de la pista
            q.transform.position = c + outN * (thick * 0.5f + 0.003f);
            q.transform.rotation = Quaternion.LookRotation(-outN, upDir);
            q.transform.localScale = new Vector3(w, h, 1f);
            q.transform.SetParent(t, true);
            var qr = q.GetComponent<MeshRenderer>();
            qr.sharedMaterial = mat;
            qr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Undo.RegisterCreatedObjectUndo(q, "Imagen de pista");
        }
    }
}
