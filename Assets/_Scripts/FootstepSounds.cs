using UnityEngine;

// Poner en Player. Los pasos cambian segun la zona (suelo del laboratorio, tierra del bosque...).
// Los clips de cada zona se asignan en AudioZone > Footstep Clips.
[RequireComponent(typeof(CharacterController))]
public class FootstepSounds : MonoBehaviour
{
    public AudioClip[] defaultClips;             // se usan si la zona no tiene pasos propios
    [Range(0f, 1f)] public float defaultVolume = 0.5f;
    public float walkInterval = 0.55f;
    public float runInterval = 0.38f;
    public float pitchVariation = 0.1f;

    CharacterController cc;
    AudioSource src;
    Vector3 lastPos;
    float timer;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        lastPos = transform.position;
    }

    void Update()
    {
        Vector3 delta = transform.position - lastPos;
        delta.y = 0f;
        float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPos = transform.position;

        if (!cc.isGrounded || speed < 0.5f)
        {
            timer = 0.05f;
            return;
        }

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        bool run = Input.GetKey(KeyCode.LeftShift);
        timer = run ? runInterval : walkInterval;
        Step(run);
    }

    void Step(bool run)
    {
        var zone = AudioZone.Current;
        AudioClip[] clips = (zone != null && zone.footstepClips != null && zone.footstepClips.Length > 0)
            ? zone.footstepClips : defaultClips;
        if (clips == null || clips.Length == 0) return;

        float vol = zone != null && zone.footstepClips != null && zone.footstepClips.Length > 0
            ? zone.footstepVolume : defaultVolume;

        src.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        src.PlayOneShot(clips[Random.Range(0, clips.Length)], vol * (run ? 1.2f : 1f));
    }
}
