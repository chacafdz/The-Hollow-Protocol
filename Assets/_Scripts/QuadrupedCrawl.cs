using UnityEngine;

/// <summary>
/// Animacion procedural en cuatro patas (como un oso o un perro) para el modelo del enemigo.
/// No usa clips: en cada frame dobla la columna hacia delante, apoya los brazos en el suelo como patas
/// delanteras y mueve las cuatro extremidades en diagonal (trote) segun la velocidad real del enemigo.
/// Busca los huesos por nombre (spinebase, belly, chest, head, Arm/Forearm/Hand, leg/sheen/foot .L/.R).
/// Se agrega sola desde <see cref="Enemy"/>.
/// </summary>
public class QuadrupedCrawl : MonoBehaviour
{
    [Header("Postura")]
    /// <summary>Inclinacion inicial de la columna en grados (se ajusta sola para que las manos toquen el suelo).</summary>
    [Range(30f, 110f)] public float bendAngle = 75f;
    /// <summary>Si es true la inclinacion se ajusta sola segun el largo de los brazos.</summary>
    public bool autoBend = true;
    /// <summary>Cuanto se abren las patas hacia los lados (0 a 0.5).</summary>
    [Range(0f, 0.5f)] public float splay = 0.15f;
    /// <summary>Cuanto levanta la cabeza para mirar al frente (0 = sigue la columna, 1 = mira recto).</summary>
    [Range(0f, 1f)] public float headUp = 0.75f;
    /// <summary>Marcalo si el modelo se dobla hacia atras en lugar de hacia delante.</summary>
    public bool flipFacing = false;

    [Header("Paso")]
    /// <summary>Amplitud del paso en grados.</summary>
    public float strideAngle = 28f;
    /// <summary>Pasos por metro recorrido.</summary>
    public float stepsPerMeter = 0.9f;
    /// <summary>Cuanto sube y baja el cuerpo al andar, en metros.</summary>
    public float bob = 0.04f;

    [Header("Jump scare")]
    /// <summary>Si es false, durante el jump scare vuelve a su postura original (de pie).</summary>
    public bool crawlDuringScare = false;

    Transform armature, spine, belly, chest, head;
    Transform armL, foreL, handL, armR, foreR, handR;
    Transform legL, shinL, footL, legR, shinR, footR;
    Transform[] bones;
    Quaternion[] rest;
    Vector3 armatureRestPos;
    Vector3 localFacing = Vector3.forward;
    Enemy enemy;
    Vector3 lastPos;
    float phase, speedSmooth, bend;
    bool ok;

    Animator anim;          // si el enemigo tiene Animator con controller (Mixamo), se usa en lugar del codigo
    bool useAnimator;

    void Update()
    {
        if (!useAnimator) return;
        Vector3 d = transform.position - lastPos; d.y = 0f;
        lastPos = transform.position;
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);
        float speed = d.magnitude / dt;
        if (speed > 20f) speed = 0f;
        speedSmooth = Mathf.Lerp(speedSmooth, speed, 1f - Mathf.Exp(-8f * dt));

        bool scare = enemy != null && enemy.state == Enemy.State.Scare && !crawlDuringScare;
        if (scare)
        {
            // jump scare de pie, con la postura original ya ajustada
            if (anim.enabled) { anim.Rebind(); anim.enabled = false; }
            return;
        }
        if (!anim.enabled) anim.enabled = true;
        anim.SetFloat("Speed", speedSmooth);
        anim.speed = speedSmooth > 0.35f ? Mathf.Clamp(speedSmooth / 3f, 0.6f, 1.8f) : 1f;
    }

    void Start()
    {
        enemy = GetComponent<Enemy>();
        lastPos = transform.position;
        anim = GetComponentInChildren<Animator>(true);
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            useAnimator = true;   // animacion de Mixamo: el codigo no toca los huesos
            return;
        }
        var all = GetComponentsInChildren<Transform>(true);
        Transform F(string n) { foreach (var t in all) if (t.name == n) return t; return null; }

        spine = F("spinebase"); belly = F("belly"); chest = F("chest"); head = F("head");
        armL = F("Arm.L"); foreL = F("Forearm.L"); handL = F("Hand.L");
        armR = F("Arm.R"); foreR = F("Forearm.R"); handR = F("Hand.R");
        legL = F("leg.L"); shinL = F("sheen.L"); footL = F("foot.L");
        legR = F("leg.R"); shinR = F("sheen.R"); footR = F("foot.R");
        armature = F("Armature");

        ok = spine != null && belly != null && armL != null && foreL != null && handL != null && armR != null && foreR != null
             && handR != null && legL != null && shinL != null && footL != null && legR != null && shinR != null && footR != null;
        if (!ok) { Debug.Log("[QuadrupedCrawl] El modelo no tiene los huesos esperados; se queda con su postura normal."); enabled = false; return; }
        if (armature == null) armature = spine.parent;

        bones = all;
        rest = new Quaternion[all.Length];
        for (int i = 0; i < all.Length; i++) rest[i] = all[i].localRotation;
        armatureRestPos = armature.localPosition;

        // hacia donde mira el modelo en reposo: de la cabeza hacia su cara
        var face = F("frontface");
        Vector3 f = Vector3.zero;
        if (face != null && head != null) f = face.position - head.position;
        f.y = 0f;
        if (f.sqrMagnitude < 1e-6f) f = transform.forward;
        localFacing = transform.InverseTransformDirection(f.normalized);

        bend = bendAngle;
        lastPos = transform.position;
    }

    void ResetPose()
    {
        for (int i = 0; i < bones.Length; i++) if (bones[i] != null) bones[i].localRotation = rest[i];
        armature.localPosition = armatureRestPos;
    }

    void LateUpdate()
    {
        if (!ok || useAnimator) return;

        // velocidad horizontal real (sirve con NavMesh o sin el)
        Vector3 d = transform.position - lastPos; d.y = 0f;
        lastPos = transform.position;
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);
        float speed = d.magnitude / dt;
        if (speed > 20f) speed = 0f;   // teletransporte (reaparicion): no cuenta como paso
        speedSmooth = Mathf.Lerp(speedSmooth, speed, 1f - Mathf.Exp(-8f * dt));

        ResetPose();
        if (enemy != null && enemy.state == Enemy.State.Scare && !crawlDuringScare) return;

        Vector3 up = Vector3.up;
        Vector3 fwd = transform.TransformDirection(localFacing); fwd.y = 0f; fwd.Normalize();
        if (flipFacing) fwd = -fwd;
        Vector3 side = Vector3.Cross(up, fwd);   // derecha del modelo

        phase += d.magnitude * stepsPerMeter * Mathf.PI * 2f;
        float moving = Mathf.Clamp01(speedSmooth / 1.5f);
        float s = Mathf.Sin(phase) * strideAngle * moving;
        float lift = Mathf.Max(0f, Mathf.Cos(phase)) * moving;
        float liftO = Mathf.Max(0f, -Mathf.Cos(phase)) * moving;

        // 1) columna hacia delante (parte en belly, parte en chest)
        Quaternion lean = Quaternion.AngleAxis(bend * 0.6f, side);
        belly.rotation = lean * belly.rotation;
        if (chest != null) chest.rotation = Quaternion.AngleAxis(bend * 0.4f, side) * chest.rotation;
        if (head != null) head.rotation = Quaternion.AngleAxis(-bend * headUp, side) * head.rotation;

        // 2) patas: trote en diagonal (brazo izq + pierna der juntos)
        Leg(armL, foreL, handL, -side, -s, lift, true);
        Leg(armR, foreR, handR, side, s, liftO, true);
        Leg(legR, shinR, footR, side, -s, lift, false);
        Leg(legL, shinL, footL, -side, s, liftO, false);

        // 3) apoya los pies traseros en el suelo y sube/baja con el paso
        float ground = transform.position.y;
        float footMin = Mathf.Min(footL.position.y, footR.position.y);
        float bobY = Mathf.Abs(Mathf.Sin(phase)) * bob * moving;
        armature.position += up * (ground - footMin + bobY);

        // 4) ajusta la inclinacion para que las manos lleguen al suelo
        if (autoBend)
        {
            float handGap = Mathf.Min(handL.position.y, handR.position.y) - ground;
            bend = Mathf.Clamp(bend + handGap * 25f * dt, 35f, 110f);
        }
    }

    // apunta un hueso de la pata hacia abajo con el balanceo del paso
    void Leg(Transform upper, Transform lower, Transform end, Vector3 outward, float swing, float lift, bool front)
    {
        Vector3 lf = transform.TransformDirection(localFacing); lf.y = 0f; lf.Normalize();
        if (flipFacing) lf = -lf;
        Vector3 side = Vector3.Cross(Vector3.up, lf);

        // muslo / brazo: hacia abajo, un poco abierto, balanceado hacia delante o atras
        float baseAng = front ? 0f : -15f;   // las traseras un poco hacia delante, como un oso agachado
        Vector3 dirUpper = Quaternion.AngleAxis(baseAng - swing, side) * Vector3.down + outward.normalized * splay;
        Aim(upper, lower, dirUpper);

        // espinilla / antebrazo: dobla la rodilla o el codo al levantar la pata
        float bendAng = front ? (-10f - lift * 35f) : (25f + lift * 35f);
        Vector3 dirLower = Quaternion.AngleAxis(baseAng - swing * 0.5f + bendAng, side) * Vector3.down;
        Aim(lower, end, dirLower);
    }

    static void Aim(Transform bone, Transform child, Vector3 desired)
    {
        Vector3 cur = child.position - bone.position;
        if (cur.sqrMagnitude < 1e-8f || desired.sqrMagnitude < 1e-8f) return;
        bone.rotation = Quaternion.FromToRotation(cur, desired) * bone.rotation;
    }
}
