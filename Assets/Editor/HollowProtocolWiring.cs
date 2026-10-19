using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Hollow Protocol > Conectar interactuables
// Busca por nombre los objetos de la torre, la casa y la cabana en la escena abierta,
// les pone Collider + script y conecta las referencias. Se puede ejecutar varias veces
// (no duplica nada) y se deshace con Ctrl+Z.
public static class HollowProtocolWiring
{
    static readonly StringBuilder log = new StringBuilder();

    [MenuItem("Tools/Hollow Protocol/Conectar interactuables")]
    public static void Wire()
    {
        log.Clear();
        var all = new List<Transform>(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        Transform casa  = FindTop(all, "casavieja", "casa");
        Transform cabana = FindTop(all, "cabana", "cabanachida");
        Transform torre = FindTop(all, "torre");
        Report("Casa vieja (destino)", casa);
        Report("Cabana (destino)", cabana);
        Report("Torre", torre);

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Conectar interactuables");

        // ---- Puertas
        var pCab = Find(all, "Puerta_Cabana");
        if (pCab != null)
        {
            EnsureCollider(pCab.gameObject);
            var d = Ensure<LockedDoor>(pCab.gameObject);
            Undo.RecordObject(d, "LockedDoor");
            d.requiredKey = "cabana";
            d.lockedMessage = "Cerrada con llave. El guardabosque la dejó en la torre.";
            d.hinge = Find(all, "Puerta_Cabana_Bisagra");
            d.prompt = "Pulsa E para abrir";
            EditorUtility.SetDirty(d);
            Ok("Puerta_Cabana -> LockedDoor (llave 'cabana', bisagra " + (d.hinge ? "OK" : "FALTA") + ")");
        }
        else Miss("Puerta_Cabana");

        var pCasa = Find(all, "Puerta_Casa");
        if (pCasa != null)
        {
            EnsureCollider(pCasa.gameObject);
            var d = Ensure<LockedDoor>(pCasa.gameObject);
            Undo.RecordObject(d, "LockedDoor");
            d.requiredKey = "";
            d.hinge = Find(all, "Puerta_Casa_Bisagra");
            d.prompt = "Pulsa E para abrir";
            EditorUtility.SetDirty(d);
            Ok("Puerta_Casa -> LockedDoor (sin llave, bisagra " + (d.hinge ? "OK" : "FALTA") + ")");
        }
        else Miss("Puerta_Casa");

        // ---- Llave y brujula (grupos con varias mallas: collider explicito)
        var llave = Find(all, "Llave_Cabana");
        if (llave != null)
        {
            SetBox(llave.gameObject, 0.4f);
            var k = Ensure<KeyPickup>(llave.gameObject);
            Undo.RecordObject(k, "KeyPickup");
            k.keyId = "cabana";
            EditorUtility.SetDirty(k);
            Ok("Llave_Cabana -> KeyPickup (keyId 'cabana')");
        }
        else Miss("Llave_Cabana");

        var bru = Find(all, "Brujula");
        if (bru != null)
        {
            SetBox(bru.gameObject, 0.3f);
            Ensure<CompassPickup>(bru.gameObject);
            Ok("Brujula -> CompassPickup");
        }
        else Miss("Brujula (el grupo hijo de BRUJULA)");

        // ---- Pistas
        Clue(all, "Pista_Radio", casa, "Casa vieja",
             "Radio: «...la casa vieja... al sureste... el registro no está ahí, pero ahí empieza el camino...»");
        Clue(all, "Pista_Mapa", cabana, "Cabaña",
             "Un mapa con una cabaña marcada en rojo. Alguien tachó el resto.");
        Clue(all, "Pista_Diario", null, "", "Diario: «Día 14. Ya no confío en lo que se mueve entre los árboles.»");
        Clue(all, "Pista_Foto", null, "", "Una foto familiar. Una de las caras está tachada.");
        Clue(all, "Pista_Nota", null, "", "Nota: «La llave se quedó en la torre. No vuelvas de noche.»");

        // ---- Registro final + EndTrigger
        var reg = Find(all, "Registro_Final");
        if (reg != null)
        {
            EnsureCollider(reg.gameObject);
            var r = Ensure<RegistroPickup>(reg.gameObject);

            var et = Object.FindFirstObjectByType<EndTrigger>(FindObjectsInactive.Include);
            if (et == null)
            {
                var go = new GameObject("EndTrigger");
                Undo.RegisterCreatedObjectUndo(go, "EndTrigger");
                var bc = go.AddComponent<BoxCollider>();
                bc.isTrigger = true;
                Bounds b = cabana != null ? GetBounds(cabana.gameObject) : GetBounds(reg.gameObject);
                go.transform.position = b.center;
                bc.size = b.size;
                et = go.AddComponent<EndTrigger>();
                Ok("Creado EndTrigger nuevo en la cabana");
            }
            Undo.RecordObject(et, "EndTrigger");
            et.triggerByEntering = false;
            EditorUtility.SetDirty(et);

            Undo.RecordObject(r, "RegistroPickup");
            r.endTrigger = et;
            EditorUtility.SetDirty(r);
            Ok("Registro_Final -> RegistroPickup (EndTrigger conectado, 'Trigger By Entering' desmarcado)");
        }
        else Miss("Registro_Final");

        // ---- Luz roja de la torre
        if (torre != null && Object.FindFirstObjectByType<BlinkLight>(FindObjectsInactive.Include) == null)
        {
            Bounds b = GetBounds(torre.gameObject);
            var go = new GameObject("Luz_Torre");
            Undo.RegisterCreatedObjectUndo(go, "Luz_Torre");
            go.transform.position = new Vector3(b.center.x, b.max.y + 1f, b.center.z);
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = Color.red;
            l.range = 60f;
            go.AddComponent<BlinkLight>();
            Ok("Creada Luz_Torre (roja, Range 60, BlinkLight) sobre la torre");
        }
        else if (torre == null) Miss("Torre (no se creó la luz roja)");

        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log("[Hollow Protocol] Conexión terminada. Guarda la escena (Ctrl+S).\n" + log);
        EditorUtility.DisplayDialog("Hollow Protocol", log.ToString() + "\nGuarda la escena con Ctrl+S.", "OK");
    }

    // ---------- helpers
    static void Clue(List<Transform> all, string name, Transform dest, string label, string text)
    {
        var t = Find(all, name);
        if (t == null) { Miss(name); return; }
        EnsureCollider(t.gameObject);
        var c = Ensure<ClueNote>(t.gameObject);
        Undo.RecordObject(c, "ClueNote");
        if (c.clueText.StartsWith("Escribe aqui")) c.clueText = text;   // no pisa tus textos
        if (dest != null) { c.pointsTo = dest; c.pointsToLabel = label; }
        EditorUtility.SetDirty(c);
        Ok(name + " -> ClueNote" + (dest ? " (señala " + label + ")" : ""));
    }

    static T Ensure<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : Undo.AddComponent<T>(go);
    }

    // Collider no-trigger si no hay ninguno en el objeto ni en sus hijos
    static void EnsureCollider(GameObject go)
    {
        if (go.GetComponentInChildren<Collider>(true) != null) return;
        var bc = Undo.AddComponent<BoxCollider>(go);
        var b = GetBounds(go);
        bc.center = go.transform.InverseTransformPoint(b.center);
        var s = go.transform.lossyScale;
        bc.size = new Vector3(b.size.x / Mathf.Max(Mathf.Abs(s.x), 1e-4f),
                              b.size.y / Mathf.Max(Mathf.Abs(s.y), 1e-4f),
                              b.size.z / Mathf.Max(Mathf.Abs(s.z), 1e-4f));
    }

    // Box Collider de lado fijo (en metros reales) centrado en las mallas del grupo
    static void SetBox(GameObject go, float side)
    {
        var bc = go.GetComponent<BoxCollider>();
        if (bc == null) bc = Undo.AddComponent<BoxCollider>(go);
        Undo.RecordObject(bc, "BoxCollider");
        bc.isTrigger = false;
        var b = GetBounds(go);
        bc.center = go.transform.InverseTransformPoint(b.center);
        var s = go.transform.lossyScale;
        bc.size = new Vector3(side / Mathf.Max(Mathf.Abs(s.x), 1e-4f),
                              side / Mathf.Max(Mathf.Abs(s.y), 1e-4f),
                              side / Mathf.Max(Mathf.Abs(s.z), 1e-4f));
        EditorUtility.SetDirty(bc);
    }

    static Bounds GetBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.3f);
        var b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    static string Norm(string s)
    {
        return s.ToLowerInvariant().Replace("ñ", "n").Replace(" ", "").Replace("_", "");
    }

    static Transform Find(List<Transform> all, string name)
    {
        string n = Norm(name);
        foreach (var t in all) if (Norm(t.name) == n) return t;
        return null;
    }

    // Objeto mas alto en la jerarquia cuyo nombre coincide, ignorando puertas, registro, pistas y bisagras
    static Transform FindTop(List<Transform> all, params string[] names)
    {
        Transform best = null; int bestDepth = int.MaxValue;
        foreach (var t in all)
        {
            string n = Norm(t.name);
            if (n.StartsWith("puerta") || n.StartsWith("registro") || n.StartsWith("pista")
                || n.StartsWith("luz") || n.Contains("bisagra")) continue;
            bool match = false;
            foreach (var want in names) if (n == want || n.StartsWith(want)) { match = true; break; }
            if (!match) continue;
            int d = 0; for (var p = t.parent; p != null; p = p.parent) d++;
            if (d < bestDepth) { best = t; bestDepth = d; }
        }
        return best;
    }

    static void Ok(string s) { log.AppendLine("OK    " + s); }
    static void Miss(string s) { log.AppendLine("FALTA " + s + " (no encontrado en la escena)"); }
    static void Report(string label, Transform t) { log.AppendLine((t ? "OK    " : "FALTA ") + label + (t ? ": " + t.name : "")); }
}
