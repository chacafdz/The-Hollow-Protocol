using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ajusta el collider de un camino hecho con Spline Extrude para que tenga exactamente la forma
/// de su malla, en lugar de una caja enorme que rodea todo el camino.
/// Se agrega solo a cada camino al cargar el nivel.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
public class SplineColliderFit : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
        AttachAll();
    }

    static void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachAll();
    }

    /// <summary>Indica si el objeto es un camino de Spline Extrude (o se llama "Spline...").</summary>
    /// <param name="t">Objeto a revisar.</param>
    public static bool IsSplinePath(Transform t)
    {
        if (t.GetComponent<MeshFilter>() == null) return false;
        return t.GetComponent("SplineExtrude") != null || t.name.StartsWith("Spline");
    }

    static void AttachAll()
    {
        foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!IsSplinePath(mf.transform)) continue;
            if (mf.GetComponent<SplineColliderFit>() == null) mf.gameObject.AddComponent<SplineColliderFit>();
        }
    }

    IEnumerator Start()
    {
        yield return null;      // espera a que Spline Extrude genere su malla
        Fit();
    }

    /// <summary>Quita los colliders de caja y deja un MeshCollider con la forma real del camino.</summary>
    public void Fit()
    {
        foreach (var c in GetComponents<Collider>())
            if (!(c is MeshCollider)) Destroy(c);

        var mesh = GetComponent<MeshFilter>().sharedMesh;
        var mc = GetComponent<MeshCollider>();
        if (mesh == null || mesh.vertexCount == 0)
        {
            if (mc != null) Destroy(mc);
            return;
        }

        if (mc == null) mc = gameObject.AddComponent<MeshCollider>();
        mc.convex = false;
        mc.sharedMesh = null;
        mc.sharedMesh = mesh;
    }
}
