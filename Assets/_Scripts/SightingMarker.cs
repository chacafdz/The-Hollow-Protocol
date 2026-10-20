using UnityEngine;

/// <summary>
/// Marca un extremo de la ruta del primer avistamiento del enemigo. Solo dibuja ayudas en la vista
/// de escena (esfera roja y linea hacia el otro extremo); no hace nada durante el juego.
/// </summary>
public class SightingMarker : MonoBehaviour
{
    /// <summary>Otro extremo de la ruta (el enemigo camina de Avistamiento_Inicio a Avistamiento_Fin).</summary>
    public Transform other;

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.1f, 0.1f);
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f, 1.2f);
        if (other != null)
        {
            Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, other.position + Vector3.up * 0.5f);
            // flecha: sentido de la caminata
            Vector3 dir = (other.position - transform.position).normalized;
            Vector3 mid = (transform.position + other.position) * 0.5f + Vector3.up * 0.5f;
            Vector3 side = new Vector3(-dir.z, 0f, dir.x) * 2f;
            Gizmos.DrawLine(mid, mid - dir * 3f + side);
            Gizmos.DrawLine(mid, mid - dir * 3f - side);
        }
    }
}
