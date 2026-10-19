using UnityEngine;

// Sonidos sueltos en momentos y lugares aleatorios (golpes lejanos, buho, ramas, aullidos...).
// Solo suenan cuando el jugador esta dentro de la zona que los controla.
public class RandomAmbientSounds : ZoneAudio
{
    [Header("Sonidos (uno o varios)")]
    public AudioClip[] clips;

    [Header("Tiempo entre sonidos (segundos)")]
    public float minInterval = 6f;
    public float maxInterval = 20f;

    [Header("Variacion")]
    [Range(0f, 1f)] public float minVolume = 0.15f;
    [Range(0f, 1f)] public float maxVolume = 0.4f;
    public float minPitch = 0.85f;
    public float maxPitch = 1.1f;

    [Header("Direccion 3D alrededor del jugador")]
    public bool spatial = true;
    public float minDistance = 6f;
    public float maxDistance = 14f;

    [Header("Zonas")]
    public bool alwaysOn = false;                // suena siempre, sin importar la zona

    bool inside;
    float nextTime;
    Transform listener;

    void OnEnable() { Schedule(0.5f); }

    public override void SetInside(bool value)
    {
        if (value && !inside) Schedule(0.5f);
        inside = value;
    }

    void Schedule(float factor = 1f)
    {
        nextTime = Time.time + Random.Range(minInterval, maxInterval) * factor;
    }

    void Update()
    {
        if (!inside && !alwaysOn) return;
        if (Time.time < nextTime) return;
        Schedule();
        Play();
    }

    void Play()
    {
        if (clips == null || clips.Length == 0) return;

        if (listener == null)
        {
            var l = FindFirstObjectByType<AudioListener>();
            listener = l != null ? l.transform : transform;
        }

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        Vector3 pos = listener.position;
        if (spatial)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            pos += new Vector3(dir.x, 0f, dir.y) * Random.Range(minDistance, maxDistance);
        }

        var go = new GameObject("SonidoAmbiente");
        go.transform.position = pos;

        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = Random.Range(minVolume, maxVolume);
        src.pitch = Random.Range(minPitch, maxPitch);
        src.spatialBlend = spatial ? 1f : 0f;
        src.minDistance = 3f;
        src.maxDistance = 30f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.Play();

        Destroy(go, clip.length / Mathf.Max(0.1f, src.pitch) + 0.5f);
    }
}
