using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enemigo "Rastreador" (GDD, seccion 5): sigue sonidos y rastros recientes. No muere.
/// </summary>
/// <remarks>
/// - Permanece oculto hasta que el jugador sale del laboratorio. Entonces se le ve UNA vez a lo lejos,
///   caminando sin notarlo. Despues desaparece un rato y ya empieza a acosarlo hasta el final.
/// - Oye los pasos (caminar poco, correr mucho, quieto = silencio) y los disparos.
/// - Ve en un cono de vision; con la linterna encendida te ve de mas lejos.
/// - Si te pierde, va al ultimo lugar donde te noto y te busca.
/// - Al tocarte o recibir suficiente dano se retira un buen rato, se oculta y regresa tranquilo.
/// - Respira con variaciones (fragmentos aleatorios de uno o varios clips).
/// - Al final del juego (EndTrigger) aparece sobre la mesa para el jump scare.
/// Necesita Health (dano de la pistola) y un Collider no-trigger. NavMeshAgent es opcional.
/// </remarks>
[RequireComponent(typeof(Health))]
public class Enemy : MonoBehaviour
{
    /// <summary>Estados de la IA del enemigo.</summary>
    public enum State { Inactive, Sighting, Dormant, Investigate, Chase, Search, Retreat, Hidden, Scare }

    [Header("Primera aparicion (al salir del laboratorio)")]
    [Tooltip("Marcado: oculto hasta que abres la puerta de salida y sales del laboratorio.")]
    public bool waitForLabExit = true;
    [Tooltip("Puerta de salida del lab. Vacio = se busca sola.")]
    public ExitDoor exitDoor;
    [Tooltip("Margen en metros alrededor del lab para contar como 'afuera'.")]
    public float leaveLabMargin = 10f;
    [Tooltip("Solo si no se encuentra el lab: distancia a la puerta para contar como afuera.")]
    public float leaveLabDistance = 18f;
    [Tooltip("A que distancia se le ve caminando (0 = 65 m). Se acorta si hay niebla.")]
    public float sightingDistance = 65f;
    [Tooltip("Cuanto tiempo camina a la vista antes de irse (0 = 25 s).")]
    public float sightingSeconds = 25f;
    public float sightingSpeed = 1.5f;
    [Tooltip("Si te acercas a menos de esto durante el avistamiento, desaparece (0 = 22 m).")]
    public float sightingTooCloseDistance = 22f;
    [Tooltip("Tiempo oculto tras el avistamiento antes de empezar a acosarte (0 = 45 s).")]
    public float firstHuntDelaySeconds = 45f;
    [Tooltip("Cada cuanto vuelve a ir hacia ti cuando esta merodeando, en segundos (minimo).")]
    public float harassMinSeconds = 45f;
    public float harassMaxSeconds = 90f;
    [Tooltip("Luz roja tenue en sus ojos: sirve para verlo de lejos en la oscuridad.")]
    public bool redGlow = true;

    [Header("Velocidades")]
    public float wanderSpeed = 1.4f;
    public float investigateSpeed = 2.4f;
    public float chaseSpeed = 3.6f;           // menor que la carrera del jugador (5.5)
    public float retreatSpeed = 7f;

    [Header("Sentidos")]
    public float sightRange = 18f;
    public float flashlightSightRange = 30f;  // la linterna encendida te delata
    [Range(20f, 180f)] public float fieldOfView = 140f;
    public float closeSenseRange = 3f;        // te siente aunque estes detras
    public float walkNoise = 8f;
    public float runNoise = 22f;
    public float shotNoise = 50f;
    public float runThreshold = 4.5f;         // velocidad a partir de la cual cuenta como correr
    public float loseRange = 70f;

    [Header("Comportamiento")]
    public bool startActive = true;           // false = no merodea, espera a oir o ver algo
    public float wanderRadius = 12f;
    public float searchSeconds = 8f;
    public float touchDistance = 1.5f;
    public float touchCooldown = 4f;
    [Tooltip("Si te persigue este tiempo sin tocarte, se cansa y te busca en vez de seguirte (0 = 20 s).")]
    public float chaseMaxSeconds = 20f;
    [TextArea] public string touchMessage = "Algo te rozó en la oscuridad…";

    [Header("Retirada (no muere)")]
    public float retreatDamage = 100f;        // con pistola de 25 = 4 disparos
    [Tooltip("Cuanto huye tras tocarte o recibir dano (0 = 8 s).")]
    public float retreatLongSeconds = 8f;
    [Tooltip("Cuanto se oculta antes de volver (0 = 40 s).")]
    public float hideLongSeconds = 40f;
    [Tooltip("Al volver, durante este tiempo solo investiga: no te persigue de inmediato (0 = 15 s).")]
    public float calmAfterReturnSeconds = 15f;
    public float respawnDistance = 35f;
    public float retreatSeconds = 4f;         // (antiguo, ya no se usa)
    public float hideSeconds = 10f;           // (antiguo, ya no se usa)

    [Header("Jump scare final")]
    [Tooltip("Que parte de la pantalla ocupa la cabeza si no hay mesa (0.6 = 60%).")]
    [Range(0.2f, 1f)] public float headScreenFill = 0.6f;
    public float faceDistanceOverride = 0f;
    public float lungeSeconds = 0.12f;
    [Tooltip("Pon 180 si el modelo aparece de espaldas.")]
    public float scareYawOffset = 0f;
    [Tooltip("Inclinacion hacia el jugador cuando aparece sobre la mesa.")]
    public float scareLeanDegrees = 22f;
    [Tooltip("Altura a la que se escala el enemigo en el jump scare si mide menos (0 = 1.7 m).")]
    public float scareTargetHeight = 1.7f;
    public float scareDistance = 1.3f;        // (antiguo, ya no se usa)
    public float scareSeconds = 1.6f;
    public float scareShake = 0.07f;

    [Header("Respiracion con variaciones")]
    [Tooltip("Uno o varios clips (por ejemplo 'demon breathing'). Se reproducen fragmentos al azar. Vacio = se genera uno.")]
    public AudioClip[] breathClips;
    public float breathMinSeconds = 1.6f;
    public float breathMaxSeconds = 3.6f;
    public float breathPauseMin = 0.6f;
    public float breathPauseMax = 3.2f;
    public float idleSoundRange = 35f;

    [Header("Sonidos (vacio = se generan por codigo)")]
    public AudioClip idleSound;               // (antiguo, ya no se usa: ver breathClips)
    public AudioClip aggroSound;              // gruñido al detectarte
    public AudioClip touchSound;
    public AudioClip scareSound;              // chillido del jump scare
    public AudioClip scareSound2;             // segundo sonido opcional que suena a la vez
    public AudioClip stepSound;
    public float stepStride = 1.7f;           // metros entre pasos
    [Range(0f, 1.5f)] public float volume = 1f;

    /// <summary>Estado actual de la IA.</summary>
    public State state { get; private set; }

    Health health;
    NavMeshAgent agent;
    PlayerController player;
    CharacterController playerCC;
    AudioSource breathSrc;
    Renderer[] renderers;
    Collider[] colliders;
    float damageTaken;
    float timer;
    float nextTouch;
    float loseTimer;
    float chaseTimer;
    float calmUntil;
    float wanderWait;
    float stepAccum;
    float flashAlpha;
    Vector3 lastPos;
    Vector3 baseScale;
    Vector3 home;
    Vector3 target;          // destino actual (merodeo / investigar / ultimo lugar visto)
    Rect labRect;            // area del laboratorio (en X,Z)
    bool hasLabRect;
    float sightTimer;
    Vector3 sightDir;
    float nextHarass;
    Light glow;

    void Awake()
    {
        health = GetComponent<Health>();
        health.destroyOnDeath = false;          // nunca debe morir
        health.Damaged += OnDamaged;            // cuenta golpes sin depender de la barra de vida
        agent = GetComponent<NavMeshAgent>();
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        baseScale = transform.localScale;
        home = transform.position;
        lastPos = transform.position;

        // un modelo con esqueleto puede calcular mal sus limites y desaparecer de cerca: se fuerza a dibujarse siempre
        foreach (var r in renderers)
            if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;

        // sonidos generados por codigo si no hay clips asignados
        if (breathClips == null || breathClips.Length == 0) breathClips = new[] { EnemyAudio.Breathing() };
        if (aggroSound == null) aggroSound = EnemyAudio.Growl();
        if (touchSound == null) touchSound = EnemyAudio.Hit();
        if (scareSound == null) scareSound = EnemyAudio.Screech();
        if (stepSound == null)  stepSound  = EnemyAudio.Step();
    }

    void OnDestroy()
    {
        if (health != null) health.Damaged -= OnDamaged;
    }

    void Start()
    {
        if (agent != null && !agent.isOnNavMesh) { agent.enabled = false; agent = null; }
        SnapToGround(true);          // pegarlo al suelo desde el primer frame
        home = transform.position;
        lastPos = transform.position;
        target = transform.position;

        CreateGlow();
        nextHarass = Time.time + 30f;

        // oculto hasta que el jugador abra la salida del lab y salga
        if (waitForLabExit && exitDoor == null) exitDoor = FindFirstObjectByType<ExitDoor>(FindObjectsInactive.Include);
        if (waitForLabExit && exitDoor != null)
        {
            BuildLabRect();
            state = State.Inactive;
            SetVisible(false);
        }
        else
        {
            state = State.Dormant;
            if (waitForLabExit) Debug.LogWarning("Enemy: no encontre la puerta de salida del lab (ExitDoor); se activa desde el inicio.");
        }

        StartCoroutine(BreathRoutine());
    }

    // area del laboratorio en X,Z a partir de los objetos labV2 y labvV2Inicio
    void BuildLabRect()
    {
        bool any = false; Bounds b = new Bounds();
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string n = t.name.ToLowerInvariant().Replace(" ", "").Replace("_", "");
            if (n != "labv2" && n != "labvv2inicio") continue;
            foreach (var r in t.GetComponentsInChildren<Renderer>(true))
            {
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
        }
        if (!any) { hasLabRect = false; return; }
        labRect = Rect.MinMaxRect(b.min.x - leaveLabMargin, b.min.z - leaveLabMargin, b.max.x + leaveLabMargin, b.max.z + leaveLabMargin);
        hasLabRect = true;
    }

    bool PlayerOutsideLab()
    {
        Vector3 pp = player.transform.position;
        if (hasLabRect) return !labRect.Contains(new Vector2(pp.x, pp.z));
        return exitDoor != null && Flat(pp - exitDoor.transform.position).magnitude > leaveLabDistance;
    }

    // luz roja tenue a la altura de la cabeza: se ve de lejos en la oscuridad
    void CreateGlow()
    {
        if (!redGlow) return;
        float headY = 1.8f;
        if (renderers != null && renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds; for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            headY = Mathf.Max(0.8f, (b.max.y - transform.position.y) - 0.3f);
        }
        var go = new GameObject("OjosRojos");
        go.transform.SetParent(transform, false);
        go.transform.position = transform.position + Vector3.up * headY;
        glow = go.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(1f, 0.1f, 0.05f);
        glow.range = 7f;
        glow.intensity = 1.6f;
        glow.shadows = LightShadows.None;
    }

    // ---------- primer avistamiento: camina a lo lejos, cruzando tu campo de vision, sin verte
    void BeginSighting()
    {
        Transform cam = player.cam.transform;
        Vector3 fwd = Flat(cam.forward);
        if (fwd.sqrMagnitude < 0.01f) fwd = Flat(player.transform.forward);
        fwd.Normalize();
        Vector3 right = new Vector3(fwd.z, 0f, -fwd.x);

        float dist = sightingDistance > 0f ? sightingDistance : 65f;
        if (RenderSettings.fog && RenderSettings.fogMode == FogMode.Linear)
            dist = Mathf.Min(dist, Mathf.Max(30f, RenderSettings.fogEndDistance * 0.7f));
        dist = Mathf.Min(dist, player.cam.farClipPlane * 0.8f);

        float side = Random.value < 0.5f ? -1f : 1f;
        sightDir = right * side;                                   // cruza de un lado a otro
        Vector3 start = player.transform.position + fwd * dist - sightDir * 20f;

        transform.position = new Vector3(start.x, transform.position.y, start.z);
        SnapToGround(true);
        transform.rotation = Quaternion.LookRotation(sightDir);
        home = transform.position;
        lastPos = transform.position;
        SetVisible(true);
        sightTimer = sightingSeconds > 0f ? sightingSeconds : 25f;
        state = State.Sighting;
        Debug.Log("[Enemy] Primer avistamiento a " + dist.ToString("0") + " m. Camina " + (sightTimer).ToString("0") + " s sin verte.");
    }

    void UpdateSighting()
    {
        sightTimer -= Time.deltaTime;
        float dist = Flat(player.transform.position - transform.position).magnitude;
        if (dist < (sightingTooCloseDistance > 0f ? sightingTooCloseDistance : 22f)) { EndSighting(); return; }

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(sightDir), Time.deltaTime * 3f);
        transform.position += sightDir * (sightingSpeed > 0f ? sightingSpeed : 1.5f) * Time.deltaTime;
        SnapToGround(false);
        UpdateSteps();
        if (sightTimer <= 0f) EndSighting();
    }

    // desaparece; tras un rato empieza a acosarte
    void EndSighting()
    {
        state = State.Hidden;
        timer = firstHuntDelaySeconds > 0f ? firstHuntDelaySeconds : 45f;
        SetVisible(false);
    }

    // Cada disparo que le pega llega aqui
    void OnDamaged(float amount)
    {
        if (state == State.Sighting) { EndSighting(); return; }    // un disparo lo espanta
        if (state == State.Inactive || state == State.Retreat || state == State.Hidden || state == State.Scare) return;
        damageTaken += amount;
        if (damageTaken >= retreatDamage) { damageTaken = 0f; StartRetreat(); return; }
        if (player != null) target = player.transform.position;
        SetState(State.Chase);
    }

    void Update()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;
            playerCC = player.GetComponent<CharacterController>();
        }
        if (state == State.Scare) return;

        // Inactivo: espera a que el jugador abra la salida y salga del laboratorio
        if (state == State.Inactive)
        {
            if (exitDoor != null && exitDoor.IsOpen && PlayerOutsideLab()) BeginSighting();
            return;
        }

        // Avistamiento: camina a lo lejos, no te ve ni te oye
        if (state == State.Sighting)
        {
            UpdateSighting();
            return;
        }

        if (state == State.Hidden)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) Respawn();
            return;
        }

        Vector3 pp = player.transform.position;
        float dist = Flat(pp - transform.position).magnitude;
        bool sees = CanSee(dist);
        bool hears = Hears(dist);
        bool calm = Time.time < calmUntil;      // acaba de volver: investiga, no persigue

        switch (state)
        {
            case State.Dormant:
                if (sees) { target = pp; SetState(calm && dist > closeSenseRange ? State.Investigate : State.Chase); break; }
                if (hears) { target = pp; SetState(State.Investigate); break; }
                // acoso: cada cierto tiempo va hacia donde estas, aunque no hagas ruido
                if (Time.time >= nextHarass)
                {
                    nextHarass = Time.time + Random.Range(harassMinSeconds, Mathf.Max(harassMinSeconds, harassMaxSeconds));
                    target = pp;
                    SetState(State.Investigate);
                    break;
                }
                if (startActive) Wander();     // si no, se queda quieto hasta oir o ver algo
                break;

            case State.Investigate:
                if (sees && !calm) { target = pp; SetState(State.Chase); break; }
                if (sees || hears) target = pp;
                MoveToward(target, investigateSpeed);
                if (Flat(target - transform.position).magnitude < 1.5f) { timer = searchSeconds; SetState(State.Search); }
                break;

            case State.Chase:
                if (dist > loseRange) { SetState(State.Dormant); break; }
                if (sees || hears) { target = pp; loseTimer = 0f; }
                else loseTimer += Time.deltaTime;
                chaseTimer += Time.deltaTime;
                MoveToward(target, chaseSpeed);
                if (dist <= touchDistance && Time.time >= nextTouch) { Touch(); break; }
                // se cansa: no te sigue para siempre
                if (chaseTimer > (chaseMaxSeconds > 0f ? chaseMaxSeconds : 20f))
                {
                    calmUntil = Time.time + 8f;
                    timer = searchSeconds; SetState(State.Search);
                    break;
                }
                if (loseTimer > 2.5f && Flat(target - transform.position).magnitude < 1.5f) { timer = searchSeconds; SetState(State.Search); }
                break;

            case State.Search:
                timer -= Time.deltaTime;
                transform.Rotate(0f, 50f * Time.deltaTime, 0f);   // mira alrededor
                if (sees) { target = pp; SetState(calm && dist > closeSenseRange ? State.Investigate : State.Chase); break; }
                if (hears) { target = pp; SetState(State.Investigate); break; }
                if (timer <= 0f) SetState(State.Dormant);
                break;

            case State.Retreat:
                timer -= Time.deltaTime;
                Vector3 away = transform.position + Flat(transform.position - pp).normalized * 20f;
                MoveToward(away, retreatSpeed);
                if (timer <= 0f || dist > respawnDistance * 1.5f) Hide();
                break;
        }

        UpdateSteps();
    }

    // ---------- sentidos
    bool CanSee(float dist)
    {
        bool flash = player.flashlight != null && player.flashlight.enabled;
        float range = flash ? flashlightSightRange : sightRange;
        if (dist > range) return false;

        Vector3 eye = transform.position + Vector3.up * 1.6f;
        Vector3 tgt = player.transform.position + Vector3.up * 1.2f;
        Vector3 dir = tgt - eye;

        bool close = dist <= closeSenseRange;
        if (!close)
        {
            float ang = Vector3.Angle(Flat(transform.forward), Flat(dir));
            if (ang > fieldOfView * 0.5f) return false;
        }

        // linea de vista libre (ignora al propio enemigo y al jugador)
        var hits = Physics.RaycastAll(eye, dir.normalized, dir.magnitude, ~0, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (h.collider.transform.IsChildOf(transform)) continue;
            if (h.collider.GetComponentInParent<PlayerController>() != null) continue;
            return false;
        }
        return true;
    }

    bool Hears(float dist)
    {
        float radius = 0f;
        float speed = playerCC != null ? Flat(playerCC.velocity).magnitude : 0f;
        if (speed > runThreshold) radius = runNoise;
        else if (speed > 0.3f) radius = walkNoise;

        // un disparo hace mucho ruido (pistola equipada y clic)
        if (Input.GetMouseButtonDown(0) && FindFirstObjectByType<PistolWeapon>() != null) radius = Mathf.Max(radius, shotNoise);

        return radius > 0f && dist <= radius;
    }

    // ---------- estados
    void SetState(State s)
    {
        if (s == State.Chase && state != State.Chase)
        {
            chaseTimer = 0f;
            Play(aggroSound, transform.position, 1f);
        }
        state = s;
    }

    void Wander()
    {
        if (Flat(target - transform.position).magnitude < 1.2f)
        {
            wanderWait -= Time.deltaTime;
            if (wanderWait <= 0f)
            {
                Vector2 r = Random.insideUnitCircle * wanderRadius;
                target = home + new Vector3(r.x, 0f, r.y);
                wanderWait = Random.Range(2f, 6f);     // pausas irregulares: no es del todo predecible
            }
        }
        else MoveToward(target, wanderSpeed);
    }

    void Touch()
    {
        nextTouch = Time.time + touchCooldown;
        if (!string.IsNullOrEmpty(touchMessage)) player.ShowMessage(touchMessage, 2.5f);
        Play(touchSound, player.cam.transform.position, 1f);
        StartCoroutine(Shake(player.cam.transform, 0.35f, 0.08f));
        StartRetreat();
    }

    void StartRetreat()
    {
        state = State.Retreat;
        timer = retreatLongSeconds > 0f ? retreatLongSeconds : 8f;
    }

    void Hide()
    {
        state = State.Hidden;
        timer = hideLongSeconds > 0f ? hideLongSeconds : 40f;
        SetVisible(false);
    }

    void Respawn()
    {
        float a = Random.value * Mathf.PI * 2f;
        float d = respawnDistance > 0f ? respawnDistance : 35f;
        Vector3 p = player.transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * d;
        if (agent != null && NavMesh.SamplePosition(p, out NavMeshHit nh, 15f, NavMesh.AllAreas)) agent.Warp(nh.position);
        else { transform.position = p; SnapToGround(true); }
        home = transform.position;
        lastPos = transform.position;
        target = transform.position;
        SetVisible(true);
        timer = searchSeconds;
        calmUntil = Time.time + (calmAfterReturnSeconds > 0f ? calmAfterReturnSeconds : 15f);
        state = State.Search;     // reaparece buscando, tranquilo: no persigue de inmediato
    }

    // ---------- movimiento
    void MoveToward(Vector3 dest, float spd)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = spd;
            agent.SetDestination(dest);
            return;
        }
        Vector3 d = Flat(dest - transform.position);
        if (d.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 6f);
        transform.position += d.normalized * spd * Time.deltaTime;
        SnapToGround(false);
    }

    void SnapToGround(bool instant)
    {
        Vector3 o = transform.position + Vector3.up * 1.2f;
        var hits = Physics.RaycastAll(o, Vector3.down, 12f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.NegativeInfinity;
        foreach (var h in hits)
        {
            if (h.collider.transform.IsChildOf(transform)) continue;
            if (h.collider.GetComponentInParent<PlayerController>() != null) continue;
            if (h.point.y > best) best = h.point.y;
        }
        // Sin collider debajo: usa la altura del Terrain activo
        var terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            float th = terrain.SampleHeight(transform.position) + terrain.transform.position.y;
            if (float.IsNegativeInfinity(best) || best < th) best = th;
        }
        if (float.IsNegativeInfinity(best)) return;
        Vector3 p = transform.position;
        p.y = instant ? best : Mathf.Lerp(p.y, best, Time.deltaTime * 12f);
        transform.position = p;
    }

    void SetVisible(bool v)
    {
        foreach (var r in renderers) if (r != null) r.enabled = v;
        foreach (var c in colliders) if (c != null) c.enabled = v;
        if (agent != null) agent.enabled = v;
        if (glow != null) glow.enabled = v;
    }

    // pasos pesados: avisan que viene antes de verlo
    void UpdateSteps()
    {
        if (state == State.Hidden || state == State.Scare || state == State.Inactive) { lastPos = transform.position; return; }
        stepAccum += Flat(transform.position - lastPos).magnitude;
        lastPos = transform.position;
        if (stepAccum >= stepStride)
        {
            stepAccum = 0f;
            Play(stepSound, transform.position, 0.9f);
        }
    }

    void Play(AudioClip clip, Vector3 pos, float vol)
    {
        if (clip != null) AudioSource.PlayClipAtPoint(clip, pos, Mathf.Clamp01(vol * volume));
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    // ---------- respiracion: fragmentos al azar con tono, volumen y pausas distintos, para que no se note el bucle
    IEnumerator BreathRoutine()
    {
        breathSrc = gameObject.AddComponent<AudioSource>();
        breathSrc.spatialBlend = 1f;
        breathSrc.maxDistance = idleSoundRange;
        breathSrc.rolloffMode = AudioRolloffMode.Linear;
        breathSrc.playOnAwake = false;
        breathSrc.loop = false;

        while (true)
        {
            bool silent = state == State.Inactive || state == State.Hidden || state == State.Scare;
            if (silent || breathClips == null || breathClips.Length == 0) { yield return new WaitForSeconds(0.5f); continue; }

            var clip = breathClips[Random.Range(0, breathClips.Length)];
            if (clip == null) { yield return new WaitForSeconds(1f); continue; }

            float wanted = Random.Range(breathMinSeconds, Mathf.Max(breathMinSeconds, breathMaxSeconds));
            float len = Mathf.Min(wanted, clip.length);
            float start = Random.Range(0f, Mathf.Max(0.01f, clip.length - len - 0.05f));

            breathSrc.clip = clip;
            breathSrc.time = start;
            breathSrc.pitch = Random.Range(0.85f, 1.1f);
            float vol = Mathf.Clamp01(volume * Random.Range(0.6f, 1f));
            breathSrc.volume = 0f;
            breathSrc.Play();

            float t = 0f, fade = Mathf.Min(0.35f, len * 0.4f);
            while (t < len && state != State.Hidden && state != State.Scare && state != State.Inactive)
            {
                t += Time.deltaTime;
                float a = Mathf.Min(1f, Mathf.Min(t / fade, (len - t) / fade));
                breathSrc.volume = vol * Mathf.Clamp01(a);
                yield return null;
            }
            breathSrc.Stop();

            // respira mas seguido cuando te persigue
            float pause = Random.Range(breathPauseMin, Mathf.Max(breathPauseMin, breathPauseMax));
            if (state == State.Chase) pause *= 0.4f;
            yield return new WaitForSeconds(pause);
        }
    }

    // ---------- jump scare (lo llama EndTrigger)
    /// <summary>
    /// Jump scare final. Si hay <paramref name="spot"/> (la mesa del registro), el enemigo aparece
    /// ENCIMA de la mesa, inclinado hacia el jugador, y se lanza hacia su cara.
    /// La posicion se corrige con los limites reales del modelo, asi que no importa donde tenga el pivote.
    /// </summary>
    /// <param name="p">Jugador.</param>
    /// <param name="spot">Objeto sobre la mesa (Registro_Final). Puede ser null.</param>
    public IEnumerator JumpScare(PlayerController p, Transform spot)
    {
        state = State.Scare;
        if (agent != null) agent.enabled = false;
        gameObject.SetActive(true);
        SetVisible(true);

        Transform cam = p.cam.transform;
        p.enabled = false;                       // el jugador ya no puede moverse ni mirar a otro lado

        // tamano: si el modelo es chico se agranda para que imponga
        transform.localScale = baseScale;
        float height = Mathf.Max(0.3f, GetBounds().size.y);
        float wanted = scareTargetHeight > 0f ? scareTargetHeight : 1.7f;
        if (height < wanted) { transform.localScale = baseScale * (wanted / height); height = wanted; }

        Vector3 startC, endC;       // centro horizontal del modelo al empezar y al terminar el salto
        float floorY, lean;

        if (spot != null)
        {
            // encima de la mesa, del lado opuesto al jugador, y se acerca al borde
            Vector3 toPlayer = Flat(cam.position - spot.position);
            if (toPlayer.sqrMagnitude < 0.01f) toPlayer = -Flat(cam.forward);
            toPlayer.Normalize();
            floorY = spot.position.y - 0.02f;               // el registro esta sobre la mesa
            startC = spot.position - toPlayer * 0.45f;
            endC   = spot.position + toPlayer * 0.25f;
            lean = scareLeanDegrees;
        }
        else
        {
            // sin mesa: pegado a la cara, a la altura de los pies del jugador
            var cc = p.GetComponent<CharacterController>();
            floorY = cc != null ? cc.bounds.min.y : cam.position.y - 1.6f;
            Vector3 f = Flat(cam.forward); if (f.sqrMagnitude < 0.01f) f = Vector3.forward; f.Normalize();
            float fill = headScreenFill > 0f ? headScreenFill : 0.6f;
            float tanHalf = Mathf.Tan(p.cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float near = Mathf.Max(0.35f, (height * 0.24f) / (fill * 2f * tanHalf));
            if (faceDistanceOverride > 0f) near = faceDistanceOverride;
            startC = cam.position + f * (near + 2.2f);
            endC   = cam.position + f * near;
            lean = 0f;
        }

        SetFacing(cam, lean, startC);
        MoveCenterTo(startC, floorY);

        Bounds nb = GetBounds();
        Debug.Log("[Enemy] Jump scare: altura " + nb.size.y.ToString("0.00") + " m, mesa=" + (spot != null ? spot.name : "(ninguna)")
                  + ", centro " + nb.center + ", suelo Y=" + floorY.ToString("0.00"));

        // sonido y destello al aparecer
        flashAlpha = 0.9f;
        if (scareSound != null)  AudioSource.PlayClipAtPoint(scareSound, cam.position, 1f);
        if (scareSound2 != null) AudioSource.PlayClipAtPoint(scareSound2, cam.position, 1f);

        // se lanza hacia la cara en una fraccion de segundo
        float t = 0f, lunge = lungeSeconds > 0f ? lungeSeconds : 0.12f;
        while (t < lunge)
        {
            t += Time.deltaTime;
            Vector3 c = Vector3.Lerp(startC, endC, Mathf.SmoothStep(0f, 1f, t / lunge));
            SetFacing(cam, lean, c);
            MoveCenterTo(c, floorY);
            LookAtFace(cam);
            yield return null;
        }

        // se queda ahi mientras tiembla la camara
        float s = 0f;
        Vector3 origin = cam.localPosition;
        while (s < scareSeconds)
        {
            s += Time.deltaTime;
            SetFacing(cam, lean, endC);
            MoveCenterTo(endC, floorY);
            LookAtFace(cam);
            cam.localPosition = origin + Random.insideUnitSphere * scareShake;
            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.deltaTime * 0.6f);
            yield return null;
        }
        cam.localPosition = origin;
    }

    // limites reales de la malla del modelo (ignora luces y particulas)
    Bounds GetBounds()
    {
        bool any = false; Bounds b = new Bounds(transform.position, Vector3.one);
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
            if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
        }
        return b;
    }

    // mueve el modelo para que el CENTRO de sus limites quede en (x,z) y su base en floorY
    void MoveCenterTo(Vector3 center, float floorY)
    {
        Bounds b = GetBounds();
        transform.position += new Vector3(center.x - b.center.x, floorY - b.min.y, center.z - b.center.z);
    }

    // mira al jugador, inclinado hacia el (scareYawOffset = 180 si el modelo se ve de espaldas)
    void SetFacing(Transform cam, float leanDegrees, Vector3 fromPoint)
    {
        Vector3 look = Flat(cam.position - fromPoint);
        if (look.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.LookRotation(look) * Quaternion.Euler(leanDegrees, scareYawOffset, 0f);
    }

    // la camara se fija en la cabeza del enemigo (parte alta de sus limites)
    void LookAtFace(Transform cam)
    {
        Bounds b = GetBounds();
        Vector3 face = new Vector3(b.center.x, b.min.y + b.size.y * 0.85f, b.center.z);
        Vector3 dir = face - cam.position;
        if (dir.sqrMagnitude > 0.0001f) cam.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }
    IEnumerator Shake(Transform cam, float seconds, float amount)
    {
        Vector3 origin = cam.localPosition;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            cam.localPosition = origin + Random.insideUnitSphere * amount;
            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.deltaTime * 0.6f);
            yield return null;
        }
        cam.localPosition = origin;
    }

    void OnGUI()
    {
        if (flashAlpha <= 0.01f) return;
        GUI.depth = 20;
        GUI.color = new Color(0.6f, 0f, 0f, flashAlpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
    }

    void Reset()
    {
        var h = GetComponent<Health>();
        if (h != null) { h.maxHealth = 100000f; h.destroyOnDeath = false; }
    }
}

// Sonidos de terror generados por codigo (no necesitan archivos). Se usan solo si no asignas clips.
static class EnemyAudio
{
    const int Rate = 22050;

    static AudioClip Make(string name, float seconds, System.Func<float, float, float> f)
    {
        int n = (int)(seconds * Rate);
        var data = new float[n];
        for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate, seconds), -1f, 1f);
        var c = AudioClip.Create(name, n, 1, Rate, false);
        c.SetData(data, 0);
        return c;
    }

    // respiracion pesada
    public static AudioClip Breathing()
    {
        float lp = 0f; var rnd = new System.Random(3);
        return Make("enemy_breath", 12f, (t, d) =>
        {
            float noise = (float)(rnd.NextDouble() * 2 - 1);
            lp += 0.08f * (noise - lp);
            float env = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * t / 2.4f - Mathf.PI / 2f);
            float drone = Mathf.Sin(2f * Mathf.PI * 48f * t) * 0.25f;
            return (lp * 3.2f * env * env + drone) * 0.55f;
        });
    }

    // gruñido grave al detectarte
    public static AudioClip Growl()
    {
        float ph = 0f; var rnd = new System.Random(5);
        return Make("enemy_growl", 1.3f, (t, d) =>
        {
            float freq = Mathf.Lerp(95f, 42f, t / d) + Mathf.Sin(t * 38f) * 6f;
            ph += freq / Rate;
            float saw = (ph % 1f) * 2f - 1f;
            float noise = (float)(rnd.NextDouble() * 2 - 1) * 0.35f;
            float env = Mathf.Clamp01(t / 0.05f) * Mathf.Pow(1f - t / d, 0.7f);
            return (float)System.Math.Tanh((saw * 0.9f + noise) * 2.2f) * env * 0.8f;
        });
    }

    // golpe seco cuando te roza
    public static AudioClip Hit()
    {
        var rnd = new System.Random(9);
        return Make("enemy_hit", 0.4f, (t, d) =>
        {
            float env = Mathf.Exp(-t * 14f);
            float thud = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(90f, 35f, t / d) * t);
            float noise = (float)(rnd.NextDouble() * 2 - 1) * 0.4f;
            return (thud + noise) * env;
        });
    }

    // chillido del jump scare: fuerte, agudo y distorsionado
    public static AudioClip Screech()
    {
        float ph = 0f; var rnd = new System.Random(11);
        return Make("enemy_screech", 1.8f, (t, d) =>
        {
            float freq = 620f + 1400f * Mathf.Sin(t * 5f) * Mathf.Sin(t * 5f) + 500f * (t / d) + Mathf.Sin(t * 60f) * 40f;
            ph += freq / Rate;
            float s = Mathf.Sin(2f * Mathf.PI * ph) + 0.5f * Mathf.Sin(4f * Mathf.PI * ph) + 0.3f * Mathf.Sin(6f * Mathf.PI * ph);
            float noise = (float)(rnd.NextDouble() * 2 - 1) * 0.5f;
            float env = Mathf.Clamp01(t / 0.01f) * (t < d - 0.4f ? 1f : Mathf.Clamp01((d - t) / 0.4f));
            return (float)System.Math.Tanh((s + noise) * 3f) * env;
        });
    }

    // paso pesado
    public static AudioClip Step()
    {
        float lp = 0f; var rnd = new System.Random(13);
        return Make("enemy_step", 0.3f, (t, d) =>
        {
            float env = Mathf.Exp(-t * 18f);
            float thud = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(70f, 30f, t / d) * t);
            lp += 0.15f * ((float)(rnd.NextDouble() * 2 - 1) - lp);
            return (thud * 0.9f + lp * 1.5f) * env;
        });
    }
}
