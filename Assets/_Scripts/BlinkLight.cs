using UnityEngine;

// Luz que pulsa suavemente. Sirve de baliza roja en lo alto de la torre para que se vea desde lejos.
[RequireComponent(typeof(Light))]
public class BlinkLight : MonoBehaviour
{
    public float minIntensity = 0f;
    public float maxIntensity = 8f;
    public float period = 2.5f;

    Light l;

    void Awake() { l = GetComponent<Light>(); }

    void Update()
    {
        float t = Mathf.PingPong(Time.time / (period / 2f), 1f);
        l.intensity = Mathf.Lerp(minIntensity, maxIntensity, t * t);
    }
}
