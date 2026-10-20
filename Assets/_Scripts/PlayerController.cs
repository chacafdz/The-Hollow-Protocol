using UnityEngine;

// Jugador en primera persona: movimiento, cámara, linterna e interacción con E.

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Referencias")]
    public Camera cam;
    public Light flashlight;

    [Header("Movimiento")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 5.5f;
    public float gravity = -20f;
    /// <summary>Altura del salto en metros (tecla Espacio).</summary>
    public float jumpHeight = 1.1f;

    [Header("Cámara")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;

    [Header("Interacción")]
    public float reach = 2.5f;

    [Header("Estado")]
    public bool hasCard = false;

    CharacterController cc;
    float pitch;
    float verticalVelocity;
    float lastGroundedTime = -10f;
    /// <summary>Si es true el jugador no puede moverse, mirar ni interactuar (final del juego).</summary>
    [HideInInspector] public bool frozen;
    Interactable current;
    string message = "";
    float messageUntil;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        if (cam == null) cam = GetComponentInChildren<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        ShowMessage("Señal de auxilio detectada. Recupera el registro del sector.", 6f);
    }

    void Update()
    {
        if (frozen) return;   // congelado: no se mueve, no mira, no interactua (los mensajes siguen)
        Look();
        Move();
        HandleFlashlight();
        HandleInteraction();
    }

    void Look()
    {
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;
        transform.Rotate(0f, mx, 0f);
        pitch = Mathf.Clamp(pitch - my, -maxLookAngle, maxLookAngle);
        cam.transform.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    void Move()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 dir = Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1f);
        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        // isGrounded refleja el ULTIMO cc.Move; por eso se lee antes de mover y se usa un solo Move por frame
        // (si se leyera despues de un Move horizontal, casi siempre daria falso y no se podria saltar)
        bool grounded = cc.isGrounded;
        if (grounded) lastGroundedTime = Time.time;
        if (grounded && verticalVelocity < 0f) verticalVelocity = -2f;

        // salto con un pequeno margen (0.12 s) para que no se pierda al pisar el borde
        bool canJump = Time.time - lastGroundedTime <= 0.12f;
        if (canJump && (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space)))
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastGroundedTime = -10f;      // evita un segundo salto en el aire
        }

        verticalVelocity += gravity * Time.deltaTime;
        Vector3 velocity = dir * speed + Vector3.up * verticalVelocity;
        cc.Move(velocity * Time.deltaTime);
    }

    void HandleFlashlight()
    {
        if (flashlight != null && Input.GetKeyDown(KeyCode.F))
            flashlight.enabled = !flashlight.enabled;
    }

    void HandleInteraction()
    {
        current = null;
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, reach, ~0, QueryTriggerInteraction.Ignore))
        {
            var it = hit.collider.GetComponentInParent<Interactable>();
            if (it != null && it.CanInteract) current = it;
        }

        if (current != null && Input.GetKeyDown(KeyCode.E))
            current.Interact(this);
    }

    public void ShowMessage(string text, float seconds = 3f)
    {
        message = text;
        messageUntil = Time.time + seconds;
    }

    void OnGUI()
    {
        // Mira
        GUI.color = new Color(1f, 1f, 1f, 0.7f);
        GUI.DrawTexture(new Rect(Screen.width / 2f - 2, Screen.height / 2f - 2, 4, 4), Texture2D.whiteTexture);

        var style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(Screen.height * 0.03f),
            wordWrap = true
        };

        // Indicación al mirar un objeto interactivo
        if (current != null)
        {
            GUI.color = new Color(0.6f, 0.9f, 1f, 1f);
            GUI.Label(new Rect(0, Screen.height * 0.58f, Screen.width, 40), current.prompt, style);
        }

        // Mensaje temporal
        if (Time.time < messageUntil)
        {
            GUI.color = Color.white;
            float mw = Screen.width * 0.7f;
            float mh = style.CalcHeight(new GUIContent(message), mw) + 10f;
            GUI.Label(new Rect(Screen.width * 0.15f, Mathf.Min(Screen.height * 0.78f, Screen.height - mh - 8f), mw, mh), message, style);
        }
    }
}