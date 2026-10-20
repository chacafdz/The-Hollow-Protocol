using UnityEngine;

/// <summary>
/// Zona rectangular (entre la torre y la cabana) donde el enemigo aparece cuando el jugador llega a la cabana.
/// Si no hay una en la escena, el enemigo crea una automaticamente a partir de la posicion de la torre y la cabana.
/// Se puede mover, girar y cambiar de tamano; en la vista de escena se dibuja como un rectangulo rojo.
/// </summary>
public class EnemySpawnZone : MonoBehaviour
{
    /// <summary>Tamano de la zona en metros: X = ancho, Y = largo (a lo largo de su eje Z).</summary>
    public Vector2 size = new Vector2(160f, 200f);

    /// <summary>Cabana: cuando el jugador se acerca a ella, el enemigo aparece en esta zona.</summary>
    public Transform cabin;

    /// <summary>Torre (solo referencia para ajustar la zona).</summary>
    public Transform tower;

    /// <summary>Posicion de la cabana (o del centro de la zona si no hay cabana).</summary>
    public Vector3 CabinPosition => cabin != null ? cabin.position : transform.position;

    /// <summary>Punto al azar dentro de la zona, a la altura del terreno.</summary>
    public Vector3 RandomPoint()
    {
        var local = new Vector3(Random.Range(-0.5f, 0.5f) * size.x, 0f, Random.Range(-0.5f, 0.5f) * size.y);
        Vector3 w = transform.TransformPoint(local);
        var terrain = Terrain.activeTerrain;
        if (terrain != null) w.y = terrain.SampleHeight(w) + terrain.transform.position.y;
        return w;
    }

    /// <summary>Indica si un punto del mundo esta dentro de la zona.</summary>
    /// <param name="world">Punto a revisar.</param>
    public bool Contains(Vector3 world)
    {
        Vector3 l = transform.InverseTransformPoint(world);
        return Mathf.Abs(l.x) <= size.x * 0.5f && Mathf.Abs(l.z) <= size.y * 0.5f;
    }

    /// <summary>
    /// Acomoda la zona entre la torre y la cabana: empieza 10 m antes de la torre, termina 30 m despues
    /// de la cabana y mide 160 m de ancho.
    /// </summary>
    /// <param name="towerPos">Posicion de la torre.</param>
    /// <param name="cabinPos">Posicion de la cabana.</param>
    public void Fit(Vector3 towerPos, Vector3 cabinPos)
    {
        Vector3 a = new Vector3(towerPos.x, 0f, towerPos.z);
        Vector3 b = new Vector3(cabinPos.x, 0f, cabinPos.z);
        Vector3 dir = (b - a);
        float dist = dir.magnitude;
        dir = dist > 0.01f ? dir / dist : Vector3.forward;

        Vector3 start = a - dir * 10f;
        Vector3 end = b + dir * 30f;
        Vector3 center = (start + end) * 0.5f;
        center.y = cabinPos.y;

        transform.position = center;
        transform.rotation = Quaternion.LookRotation(dir);
        size = new Vector2(160f, dist + 40f);
    }

    /// <summary>Devuelve la zona de la escena; si no existe, la crea entre la torre y la cabana.</summary>
    public static EnemySpawnZone Ensure()
    {
        var zone = FindFirstObjectByType<EnemySpawnZone>(FindObjectsInactive.Include);
        if (zone != null) return zone;

        Transform tower = null, cabin = null;
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string n = t.name.ToLowerInvariant().Replace("ñ", "n").Replace(" ", "").Replace("_", "");
            if (n == "torre") tower = t;
            else if (n == "cabana" || n == "cabanachida") cabin = t;
        }
        if (cabin == null) return null;

        var go = new GameObject("Zona_Aparicion_Enemigo");
        zone = go.AddComponent<EnemySpawnZone>();
        zone.cabin = cabin;
        zone.tower = tower;
        zone.Fit(tower != null ? tower.position : cabin.position - Vector3.forward * 150f, cabin.position);
        return zone;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.9f);
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, 2f, size.y));
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.08f);
        Gizmos.DrawCube(Vector3.zero, new Vector3(size.x, 0.1f, size.y));
        Gizmos.matrix = old;
    }
}
