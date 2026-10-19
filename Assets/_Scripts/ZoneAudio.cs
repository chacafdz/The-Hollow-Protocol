using UnityEngine;

// Base para todo sonido que depende de la zona (laboratorio, bosque...).
public abstract class ZoneAudio : MonoBehaviour
{
    public abstract void SetInside(bool inside);
}
