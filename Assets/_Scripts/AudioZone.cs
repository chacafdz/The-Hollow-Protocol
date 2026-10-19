using System.Collections.Generic;
using UnityEngine;

// Zona de sonido. Poner en un objeto vacio con un Box Collider (se marca solo como Trigger).
// Cuando el jugador esta dentro, activa sus capas de audio, cambia los pasos y, si quieres, la niebla.
// Si el jugador esta en dos zonas a la vez, gana la de mayor Priority.
public class AudioZone : MonoBehaviour
{
    public static AudioZone Current;

    public string zoneName = "Zona";
    public int priority = 0;

    [Header("Sonidos que se activan dentro de esta zona")]
    public ZoneAudio[] layers;

    [Header("Pasos en esta zona")]
    public AudioClip[] footstepClips;
    [Range(0f, 1f)] public float footstepVolume = 0.5f;

    [Header("Niebla (opcional)")]
    public bool changeFog = false;
    public float fogDensity = 0.03f;

    static readonly List<AudioZone> all = new List<AudioZone>();
    Collider col;
    Transform player;
    bool active;

    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    void OnEnable() { all.Add(this); col = GetComponent<Collider>(); }

    void OnDisable()
    {
        all.Remove(this);
        if (Current == this) Current = null;
    }

    bool Contains()
    {
        return player != null && col != null && col.bounds.Contains(player.position + Vector3.up);
    }

    void Update()
    {
        if (player == null)
        {
            var p = FindFirstObjectByType<PlayerController>();
            if (p == null) return;
            player = p.transform;
        }

        bool inside = Contains();
        if (inside)
        {
            foreach (var z in all)
                if (z != this && z.priority > priority && z.Contains()) { inside = false; break; }
        }

        if (inside != active)
        {
            active = inside;
            if (active) Current = this;
            else if (Current == this) Current = null;

            foreach (var l in layers)
                if (l != null) l.SetInside(active);
        }

        if (active && changeFog)
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, fogDensity, Time.deltaTime * 1.5f);
    }
}
