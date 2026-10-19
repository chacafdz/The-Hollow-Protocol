using UnityEngine;

// Sonido en bucle con fundido de entrada y salida segun la zona.
// Sirve para: zumbido del laboratorio, grillos y ambiente nocturno, viento entre los arboles.
// Con "Gusts" el volumen sube y baja suavemente, como rafagas de viento.
[RequireComponent(typeof(AudioSource))]
public class LoopLayer : ZoneAudio
{
    public AudioClip clip;                       // si lo dejas vacio usa el clip del Audio Source
    [Range(0f, 1f)] public float volume = 0.4f;
    public float fadeTime = 2.5f;

    [Header("Rafagas (ideal para viento entre los arboles)")]
    public bool gusts = false;
    [Range(0f, 1f)] public float gustAmount = 0.5f;
    public float gustSpeed = 0.15f;

    [Header("Zonas")]
    public bool alwaysOn = false;                // suena siempre, sin importar la zona

    AudioSource src;
    float target, level, seed;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        if (clip != null) src.clip = clip;
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.volume = 0f;
        src.Stop();
        seed = Random.value * 100f;
        target = alwaysOn ? 1f : 0f;
        level = target;
    }

    public override void SetInside(bool inside)
    {
        target = (inside || alwaysOn) ? 1f : 0f;
    }

    void Update()
    {
        level = Mathf.MoveTowards(level, target, Time.deltaTime / Mathf.Max(0.01f, fadeTime));

        float g = 1f;
        if (gusts)
            g = 1f - gustAmount * (1f - Mathf.PerlinNoise(Time.time * gustSpeed, seed));

        src.volume = volume * level * g;

        if (level > 0f && !src.isPlaying && src.clip != null) src.Play();
        else if (level <= 0f && src.isPlaying) src.Stop();
    }
}
