using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Hollow Protocol > ...
public static class HollowProtocolFixes
{
    // El herraje, el candado y los pomos estan en otro grupo y no giran con la puerta.
    // Se registran en LockedDoor.attachedParts para que giren con ella.
    [MenuItem("Tools/Hollow Protocol/Pegar herrajes a las puertas")]
    public static void AttachDoorParts()
    {
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        string report = "";
        report += AttachTo(all, "Puerta_Cabana", new[] { "K_Puerta_herraje", "K_Candado", "K_Pomo" });
        report += AttachTo(all, "Puerta_Casa", new[] { "C_Pomo" });
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Hollow Protocol]\n" + report);
        EditorUtility.DisplayDialog("Hollow Protocol", report + "\nGuarda con Ctrl+S.", "OK");
    }

    // Asigna al enemigo los audios que ya tienes en Assets/_Audio:
    // respiracion (demon breathing), gruñido, chillido, jump scare y golpe.
    [MenuItem("Tools/Hollow Protocol/Asignar sonidos al enemigo")]
    public static void AssignEnemySounds()
    {
        var enemy = Object.FindFirstObjectByType<Enemy>(FindObjectsInactive.Include);
        if (enemy == null) { EditorUtility.DisplayDialog("Hollow Protocol", "No hay ningún Enemy en la escena. Crea el enemigo primero.", "OK"); return; }

        AudioClip breath = FindClip("demon-breathing");
        AudioClip growl  = FindClip("monster-growl");
        AudioClip scream = FindClip("scary-scream");
        AudioClip scare2 = FindClip("jump-scare-sound");
        AudioClip hit    = FindClip("golpe-fuerte");

        var so = new SerializedObject(enemy);
        string report = "";
        if (breath != null)
        {
            var arr = so.FindProperty("breathClips");
            arr.arraySize = 1;
            arr.GetArrayElementAtIndex(0).objectReferenceValue = breath;
            report += "OK    respiración: " + breath.name + " (se reproduce en fragmentos con variaciones)\n";
        }
        else report += "FALTA 'demon breathing' en Assets/_Audio\n";

        report += SetClip(so, "aggroSound", growl, "gruñido");
        report += SetClip(so, "scareSound", scream, "chillido del jump scare");
        report += SetClip(so, "scareSound2", scare2, "sonido extra del jump scare");
        report += SetClip(so, "touchSound", hit, "golpe al tocarte");
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(enemy);
        EditorSceneManager.MarkSceneDirty(enemy.gameObject.scene);
        Debug.Log("[Hollow Protocol] Sonidos del enemigo\n" + report);
        EditorUtility.DisplayDialog("Hollow Protocol", report + "\nGuarda con Ctrl+S.", "OK");
    }

    static AudioClip FindClip(string namePart)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant().Contains(namePart))
                return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        return null;
    }

    static string SetClip(SerializedObject so, string field, AudioClip clip, string label)
    {
        if (clip == null) return "FALTA " + label + "\n";
        so.FindProperty(field).objectReferenceValue = clip;
        return "OK    " + label + ": " + clip.name + "\n";
    }

    // Pone la caja de municion (6 balas) arriba de la cama de la torre, fija en la escena para poder moverla.
    // (Si no la haces, el juego la coloca sola al empezar.)
    [MenuItem("Tools/Hollow Protocol/Crear caja de municion en la torre")]
    public static void CreateAmmoBox()
    {
        Transform mattress = null, pillow = null;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == "T_Cama_colchon") mattress = t;
            else if (t.name == "T_Cama_almohada") pillow = t;
        }
        if (mattress == null)
        {
            EditorUtility.DisplayDialog("Hollow Protocol", "No encontré la cama de la torre (T_Cama_colchon) en esta escena. Abre SampleScene.", "OK");
            return;
        }

        var old = GameObject.Find(AmmoBox.BoxName);
        if (old != null) Undo.DestroyObjectImmediate(old);

        var box = AmmoBox.Create(mattress, pillow, true);
        Undo.RegisterCreatedObjectUndo(box, "Caja de munición");
        Selection.activeGameObject = box;
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Hollow Protocol] Caja de munición creada en " + box.transform.position + " con " + AmmoBox.Bullets + " balas.");
        EditorUtility.DisplayDialog("Hollow Protocol", "Caja con " + AmmoBox.Bullets + " balas creada sobre la cama de la torre.\nPuedes moverla; guarda con Ctrl+S.", "OK");
    }

    // Quita los colliders enormes de los caminos (Spline Extrude) y les agrega el ajuste automatico a su forma real.
    [MenuItem("Tools/Hollow Protocol/Ajustar colliders de los caminos (splines)")]
    public static void FitSplineColliders()
    {
        int paths = 0; string report = "";
        foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var t = mf.transform;
            if (!SplineColliderFit.IsSplinePath(t)) continue;

            foreach (var c in t.GetComponents<Collider>())
            {
                report += "  quitado " + c.GetType().Name + " de " + t.name + "\n";
                Undo.DestroyObjectImmediate(c);
            }
            if (t.GetComponent<SplineColliderFit>() == null) Undo.AddComponent<SplineColliderFit>(t.gameObject);
            paths++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        string msg = paths + " camino(s) ajustado(s).\n" + (report.Length > 0 ? report : "  (no tenían colliders)\n")
                   + "\nEn Play, cada camino recibe un MeshCollider con su forma exacta.\nGuarda con Ctrl+S.";
        Debug.Log("[Hollow Protocol] Colliders de caminos\n" + msg);
        EditorUtility.DisplayDialog("Hollow Protocol", msg, "OK");
    }
    // Crea (o reutiliza) los dos puntos de la ruta del primer avistamiento: Avistamiento_Inicio y Avistamiento_Fin.
    // Los coloca cruzando el camino (spline) mas cercano al jugador; despues los arrastras a donde quieras.
    [MenuItem("Tools/Hollow Protocol/Crear puntos de avistamiento del enemigo")]
    public static void CreateSightingMarkers()
    {
        var player = Object.FindFirstObjectByType<PlayerController>();
        Vector3 from = player != null ? player.transform.position : Vector3.zero;

        System.Func<Vector3, Vector3> flat = v => new Vector3(v.x, 0f, v.z);
        Vector3 nearEnd = Vector3.zero, farEnd = Vector3.zero;
        float best = float.MaxValue;
        bool found = false;

        foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!SplineColliderFit.IsSplinePath(mf.transform) || mf.sharedMesh == null) continue;
            var verts = mf.sharedMesh.vertices;
            if (verts.Length < 4) continue;

            var pts = new Vector3[verts.Length];
            Vector3 c = Vector3.zero;
            for (int i = 0; i < verts.Length; i++) { pts[i] = mf.transform.TransformPoint(verts[i]); c += pts[i]; }
            c /= pts.Length;

            Vector3 e0 = Farthest(pts, c), e1 = Farthest(pts, e0);      // los dos extremos del camino
            float d0 = (flat(e0) - flat(from)).magnitude, d1 = (flat(e1) - flat(from)).magnitude;
            float d = Mathf.Min(d0, d1);
            if (d < best)
            {
                best = d; found = true;
                nearEnd = d0 <= d1 ? e0 : e1;
                farEnd  = d0 <= d1 ? e1 : e0;
            }
        }

        Vector3 along, mid;
        if (found)
        {
            along = flat(farEnd - nearEnd).normalized;
            mid = nearEnd + (farEnd - nearEnd) * 0.28f;       // a poco menos de un tercio del camino
        }
        else
        {
            along = player != null ? flat(player.transform.forward).normalized : Vector3.forward;
            mid = from + along * 45f;
        }
        Vector3 perp = new Vector3(-along.z, 0f, along.x);
        Vector3 a = Ground(mid - perp * 30f), b = Ground(mid + perp * 30f);

        var start = GetOrCreate("Avistamiento_Inicio", a);
        var end = GetOrCreate("Avistamiento_Fin", b);
        var marker = start.GetComponent<SightingMarker>();
        if (marker == null) marker = Undo.AddComponent<SightingMarker>(start);
        marker.other = end.transform;
        EditorUtility.SetDirty(marker);

        Selection.objects = new Object[] { start, end };
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        string msg = "Puntos creados" + (found ? " cruzando el camino más cercano a ti" : " frente al jugador") + ".\n\n"
                   + "El enemigo camina de Avistamiento_Inicio a Avistamiento_Fin (la línea roja que ves en la escena).\n"
                   + "Arrástralos a donde quieras y guarda con Ctrl+S.\n\n"
                   + "Empieza a caminar cuando sales del lab, estás a menos de 70 m del centro de la ruta y miras hacia ella.";
        Debug.Log("[Hollow Protocol] " + msg);
        EditorUtility.DisplayDialog("Hollow Protocol", msg, "OK");
    }

    static Vector3 Farthest(Vector3[] pts, Vector3 from)
    {
        Vector3 best = pts[0]; float bd = -1f;
        foreach (var p in pts)
        {
            float d = new Vector2(p.x - from.x, p.z - from.z).sqrMagnitude;
            if (d > bd) { bd = d; best = p; }
        }
        return best;
    }

    static Vector3 Ground(Vector3 p)
    {
        var t = Terrain.activeTerrain;
        if (t != null) p.y = t.SampleHeight(p) + t.transform.position.y;
        return p;
    }

    static GameObject GetOrCreate(string name, Vector3 pos)
    {
        var go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Punto de avistamiento");
        }
        else Undo.RecordObject(go.transform, "Mover punto de avistamiento");
        go.transform.position = pos;
        return go;
    }
    // Crea (o recalcula) la zona rectangular roja entre la torre y la cabaña donde el enemigo aparece
    // cuando llegas a la cabaña. Sale de las posiciones de la torre y la cabaña; despues la mueves o la estiras.
    [MenuItem("Tools/Hollow Protocol/Crear zona de aparicion del enemigo (torre-cabaña)")]
    public static void CreateSpawnZone()
    {
        Transform tower = null, cabin = null;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string n = t.name.ToLowerInvariant().Replace("ñ", "n").Replace(" ", "").Replace("_", "");
            if (n == "torre") tower = t;
            else if (n == "cabana" || n == "cabanachida") cabin = t;
        }
        if (cabin == null)
        {
            EditorUtility.DisplayDialog("Hollow Protocol", "No encontré la cabaña en esta escena. Abre SampleScene.", "OK");
            return;
        }

        var zone = Object.FindFirstObjectByType<EnemySpawnZone>(FindObjectsInactive.Include);
        if (zone == null)
        {
            var go = new GameObject("Zona_Aparicion_Enemigo");
            Undo.RegisterCreatedObjectUndo(go, "Zona de aparición");
            zone = go.AddComponent<EnemySpawnZone>();
        }
        Undo.RecordObject(zone.transform, "Zona de aparición");
        Undo.RecordObject(zone, "Zona de aparición");
        zone.cabin = cabin;
        zone.tower = tower;
        zone.Fit(tower != null ? tower.position : cabin.position - Vector3.forward * 150f, cabin.position);
        EditorUtility.SetDirty(zone);

        Selection.activeGameObject = zone.gameObject;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        string msg = "Zona creada entre la torre y la cabaña.\n\n"
                   + "Centro: " + zone.transform.position + "\nAncho x largo: " + zone.size.x + " x " + zone.size.y + " m\n\n"
                   + "Muévela con la herramienta Mover, gírala o cambia 'Size' en el Inspector para que coincida con tu dibujo rojo. Guarda con Ctrl+S.\n"
                   + "Cuando llegues a menos de 60 m de la cabaña, el enemigo aparece dentro de este rectángulo.";
        Debug.Log("[Hollow Protocol] " + msg);
        EditorUtility.DisplayDialog("Hollow Protocol", msg, "OK");
    }
    static string AttachTo(Transform[] all, string doorName, string[] partNames)
    {
        var door = FindExact(all, doorName);
        if (door == null) return "FALTA " + doorName + " (no está en la escena)\n";

        // Unity no deja cambiar el padre de objetos dentro de un FBX en la escena,
        // asi que la puerta los gira junto con ella (LockedDoor.attachedParts).
        var lockedDoor = door.GetComponent<LockedDoor>();
        if (lockedDoor == null) return "FALTA LockedDoor en " + doorName + ". Corre primero 'Conectar interactuables'.\n";

        var list = new System.Collections.Generic.List<Transform>();
        string r = "";
        foreach (var name in partNames)
        {
            var part = FindExact(all, name);
            if (part == null) { r += "FALTA " + name + "\n"; continue; }
            if (part.IsChildOf(door)) { r += "Ya es hijo de la puerta: " + name + "\n"; continue; }
            list.Add(part);
            r += "OK    " + name + " gira con " + doorName + "\n";
        }

        Undo.RecordObject(lockedDoor, "Piezas de la puerta");
        lockedDoor.attachedParts = list.ToArray();
        EditorUtility.SetDirty(lockedDoor);
        return r;
    }

    // Selecciona el modelo del enemigo (en la escena, o el FBX/prefab de Project) y ejecuta esto.
    // Sin seleccion crea una capsula de prueba.
    [MenuItem("Tools/Hollow Protocol/Preparar enemigo (seleccionado)")]
    public static void PrepareEnemy()
    {
        GameObject go = Selection.activeGameObject;
        if (go != null && EditorUtility.IsPersistent(go))
        {
            Vector3 pos = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(go);
            inst.transform.position = pos;
            Undo.RegisterCreatedObjectUndo(inst, "Crear enemigo");
            go = inst;
        }
        else if (go == null)
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Enemigo (prueba)";
            Vector3 pos = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
            go.transform.position = pos + Vector3.up;
            go.transform.localScale = new Vector3(1f, 1.1f, 1f);
            Undo.RegisterCreatedObjectUndo(go, "Crear enemigo");
        }

        // Collider (las balas lo necesitan; no puede ser trigger)
        if (go.GetComponentInChildren<Collider>(true) == null)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(go.transform.position, new Vector3(1, 2, 1));
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            var cc = Undo.AddComponent<CapsuleCollider>(go);
            cc.center = go.transform.InverseTransformPoint(b.center);
            var s = go.transform.lossyScale;
            cc.height = b.size.y / Mathf.Max(Mathf.Abs(s.y), 1e-4f);
            cc.radius = Mathf.Min(b.size.x, b.size.z) * 0.5f / Mathf.Max(Mathf.Abs(s.x), 1e-4f);
        }

        var h = go.GetComponent<Health>();
        if (h == null) h = Undo.AddComponent<Health>(go);
        Undo.RecordObject(h, "Health enemigo");
        h.maxHealth = 100000f;          // el enemigo no depende de esto: cuenta sus golpes por separado
        h.destroyOnDeath = false;
        EditorUtility.SetDirty(h);

        if (go.GetComponent<Enemy>() == null) Undo.AddComponent<Enemy>(go);

        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Hollow Protocol] Enemigo listo: " + go.name + ". Ajusta Speed, Start Active y los sonidos en el Inspector.");
    }

    // Modelo propio hecho por codigo: figura alta y flaca, brazos largos con garras, ojos rojos brillantes.
    // Despues pasa por CreateEnemy (collider, Health, Enemy, posicion y prefab).
    [MenuItem("Tools/Hollow Protocol/Crear enemigo Rastreador (modelo propio)")]
    public static void CreateEnemyWithOwnModel()
    {
        Material skin = MakeMat("Assets/_Materials/Rastreador_Piel.mat", new Color(0.10f, 0.09f, 0.09f), 0.25f, false);
        Material eye  = MakeMat("Assets/_Materials/Rastreador_Ojos.mat", new Color(1f, 0.05f, 0.02f), 0.8f, true);

        var root = new GameObject("Enemigo_Rastreador");
        Undo.RegisterCreatedObjectUndo(root, "Modelo enemigo");

        // torso, pelvis, cuello, cabeza (pivote del grupo en los pies)
        Part(root, PrimitiveType.Capsule, "Torso",  new Vector3(0f, 1.35f, 0f),     new Vector3(0.42f, 0.55f, 0.28f), new Vector3(10f, 0f, 0f), skin);
        Part(root, PrimitiveType.Sphere,  "Pelvis", new Vector3(0f, 0.85f, 0f),     new Vector3(0.36f, 0.28f, 0.26f), Vector3.zero, skin);
        Part(root, PrimitiveType.Cylinder,"Cuello", new Vector3(0f, 1.82f, 0.05f),  new Vector3(0.09f, 0.14f, 0.09f), new Vector3(15f, 0f, 0f), skin);
        Part(root, PrimitiveType.Sphere,  "Cabeza", new Vector3(0f, 2.0f, 0.1f),    new Vector3(0.26f, 0.34f, 0.28f), Vector3.zero, skin);
        Part(root, PrimitiveType.Sphere,  "Ojo_I",  new Vector3(-0.07f, 2.04f, 0.23f), Vector3.one * 0.055f, Vector3.zero, eye);
        Part(root, PrimitiveType.Sphere,  "Ojo_D",  new Vector3(0.07f, 2.04f, 0.23f),  Vector3.one * 0.055f, Vector3.zero, eye);

        // piernas
        foreach (float sx in new[] { -1f, 1f })
        {
            Part(root, PrimitiveType.Capsule, "Muslo", new Vector3(0.14f * sx, 0.62f, 0.02f), new Vector3(0.13f, 0.3f, 0.13f), Vector3.zero, skin);
            Part(root, PrimitiveType.Capsule, "Pierna", new Vector3(0.15f * sx, 0.22f, 0.0f), new Vector3(0.10f, 0.24f, 0.10f), Vector3.zero, skin);
        }

        // brazos largos que cuelgan casi hasta el suelo, con garras
        foreach (float sx in new[] { -1f, 1f })
        {
            Part(root, PrimitiveType.Capsule, "Brazo", new Vector3(0.36f * sx, 1.1f, 0.05f), new Vector3(0.09f, 0.55f, 0.09f), new Vector3(0f, 0f, 6f * sx), skin);
            Part(root, PrimitiveType.Sphere, "Mano", new Vector3(0.42f * sx, 0.5f, 0.1f), new Vector3(0.11f, 0.13f, 0.09f), Vector3.zero, skin);
            for (int i = -1; i <= 1; i++)
                Part(root, PrimitiveType.Capsule, "Garra", new Vector3((0.42f + 0.03f * i) * sx, 0.34f, 0.12f), new Vector3(0.018f, 0.1f, 0.018f), new Vector3(8f, 0f, 0f), skin);
        }

        // espinas en la espalda
        for (int i = 0; i < 5; i++)
            Part(root, PrimitiveType.Cube, "Espina", new Vector3(0f, 1.1f + i * 0.17f, -0.17f - i * 0.005f), new Vector3(0.04f, 0.12f, 0.05f), new Vector3(-25f, 0f, 0f), skin);

        Selection.activeGameObject = root;
        CreateEnemy();
    }

    static GameObject Part(GameObject parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Vector3 euler, Material m)
    {
        var p = GameObject.CreatePrimitive(type);
        p.name = name;
        Object.DestroyImmediate(p.GetComponent<Collider>());   // un solo collider de capsula lo pone PrepareEnemy
        p.transform.SetParent(parent.transform, false);
        p.transform.localPosition = pos;
        p.transform.localScale = scale;
        p.transform.localEulerAngles = euler;
        p.GetComponent<Renderer>().sharedMaterial = m;
        return p;
    }

    static Material MakeMat(string path, Color color, float smooth, bool emissive)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh);
        m.color = color;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (emissive)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", color * 4f);
        }
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    // Crea el Rastreador completo en la escena: usa el modelo seleccionado, o Pumpkinhulk L Shaw,
    // lo coloca a medio camino entre la torre y la cabana, y guarda un prefab en _Prefabs/Enemigo.prefab
    [MenuItem("Tools/Hollow Protocol/Crear enemigo Rastreador en la escena")]
    public static void CreateEnemy()
    {
        GameObject model = Selection.activeGameObject;
        if (model == null) model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Models/Pumpkinhulk L Shaw.fbx");

        GameObject go;
        if (model != null && EditorUtility.IsPersistent(model)) go = (GameObject)PrefabUtility.InstantiatePrefab(model);
        else if (model != null) go = model;
        else { go = GameObject.CreatePrimitive(PrimitiveType.Capsule); go.transform.localScale = new Vector3(1f, 1.1f, 1f); }
        go.name = "Enemigo_Rastreador";
        Undo.RegisterCreatedObjectUndo(go, "Crear enemigo");

        // posicion: entre la torre y la cabana
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Transform torre = null, cabana = null;
        foreach (var t in all)
        {
            string n = t.name.ToLowerInvariant().Replace("ñ", "n");
            if (torre == null && n == "torre") torre = t;
            if (cabana == null && (n == "cabana" || n == "cabanachida")) cabana = t;
        }
        Vector3 pos = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        if (torre != null && cabana != null) pos = (torre.position + cabana.position) * 0.5f;
        else if (cabana != null) pos = cabana.position + new Vector3(25f, 0f, 0f);
        if (Physics.Raycast(pos + Vector3.up * 200f, Vector3.down, out RaycastHit hit, 400f)) pos = hit.point;
        else if (Terrain.activeTerrain != null)
            pos.y = Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.transform.position.y;
        go.transform.position = pos;

        Selection.activeGameObject = go;
        PrepareEnemy();   // collider + Health inmortal + Enemy

        // tamano razonable (humanoide ~1.9 m) si el modelo vino muy grande o pequeno
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length > 0)
        {
            Bounds b = rs[0].bounds; for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            if (b.size.y < 0.5f || b.size.y > 6f) Debug.LogWarning("[Hollow Protocol] El enemigo mide " + b.size.y.ToString("0.0") + " m. Ajusta su Scale.");
        }

        string path = "Assets/_Prefabs/Enemigo.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.UserAction);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Hollow Protocol] Enemigo Rastreador creado en " + pos + " y guardado como " + path + ". Guarda la escena (Ctrl+S).");
    }

    static Transform FindExact(Transform[] all, string name)
    {
        foreach (var t in all) if (t.name == name) return t;
        return null;
    }
}
