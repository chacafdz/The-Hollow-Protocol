using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Final del juego: poner en un objeto vacio con Box Collider dentro de la cabana.
// Muestra el mensaje final, suena el golpe, funde a negro y detiene al jugador.
/// <summary>
/// Final del juego: mensaje, jump scare del enemigo sobre la mesa, fundido a negro y pantalla de fin
/// con botones para volver al menu o jugar de nuevo.
/// </summary>
public class EndTrigger : MonoBehaviour
{
    [TextArea] public string finalMessage = "Registro recuperado. Movimiento detectado detrás de ti…";
    public string endTitle = "FIN";
    public AudioClip sting;                      // golpe fuerte o sonido repentino
    public float delay = 4f;                     // segundos antes de empezar el fundido
    public float fadeTime = 3f;
    [Tooltip("Desmarca esto si el final lo dispara RegistroPickup al tomar el registro")]
    public bool triggerByEntering = true;
    [Tooltip("Enemigo para el jump scare final. Si esta vacio se busca solo en la escena.")]
    /// <summary>Enemigo que hace el jump scare. Vacio = se busca solo en la escena.</summary>
    public Enemy enemy;
    [Tooltip("Donde aparece el enemigo en el jump scare (la mesa o el Registro_Final). Vacio = se busca solo.")]
    public Transform scareSpot;

    Collider col;
    PlayerController player;
    bool done;
    float alpha;

    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    void Awake() { col = GetComponent<Collider>(); }

    void Update()
    {
        if (done || !triggerByEntering) return;
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;
        }
        if (col.bounds.Contains(player.transform.position + Vector3.up))
        {
            done = true;
            StartCoroutine(Finish());
        }
    }

    /// <summary>Inicia la secuencia final.</summary>
    /// <param name="p">Jugador.</param>
    public void Begin(PlayerController p)
    {
        if (done) return;
        done = true;
        player = p;
        StartCoroutine(Finish());
    }

    IEnumerator Finish()
    {
        player.ShowMessage(finalMessage, delay + fadeTime);
        if (sting != null) AudioSource.PlayClipAtPoint(sting, player.cam.transform.position);

        yield return new WaitForSeconds(delay);

        // Jump scare del enemigo justo antes del fundido a negro
        if (enemy == null) enemy = FindFirstObjectByType<Enemy>(FindObjectsInactive.Include);
        if (enemy != null)
        {
            enemy.gameObject.SetActive(true);
            if (scareSpot == null)
            {
                var reg = GameObject.Find("Registro_Final");
                if (reg != null) scareSpot = reg.transform;
            }
            Debug.Log("EndTrigger: jump scare con " + enemy.name + (scareSpot != null ? " sobre " + scareSpot.name : " (sin mesa)"));
            yield return enemy.JumpScare(player, scareSpot);
        }
        else Debug.LogWarning("EndTrigger: no hay ningun Enemy en la escena, no hay jump scare.");

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            alpha = Mathf.Clamp01(t / fadeTime);
            yield return null;
        }
        alpha = 1f;

        player.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnGUI()
    {
        if (alpha <= 0f) return;
        GUI.depth = 10;   // detras del mensaje del jugador
        GUI.color = new Color(0f, 0f, 0f, alpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

        if (alpha >= 1f)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(Screen.height * 0.08f)
            };
            GUI.color = Color.white;
            GUI.Label(new Rect(0, Screen.height * 0.3f, Screen.width, Screen.height * 0.2f), endTitle, style);
            // botones para volver al menu o jugar de nuevo
            var btn = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(Screen.height * 0.035f) };
            float bw = Screen.width * 0.22f, bh = Screen.height * 0.08f;
            if (GUI.Button(new Rect(Screen.width * 0.5f - bw - 10f, Screen.height * 0.6f, bw, bh), "Volver al menú", btn))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene("MainMenu");
            }
            if (GUI.Button(new Rect(Screen.width * 0.5f + 10f, Screen.height * 0.6f, bw, bh), "Jugar de nuevo", btn))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
    }
}
