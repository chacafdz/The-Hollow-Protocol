using UnityEngine;

// Se agrega solo en tiempo de ejecucion desde PistolPickup.
// Disparo minimo: sonido, destello y retroceso. Sin dano ni municion (fuera del alcance del GDD).
public class PistolWeapon : MonoBehaviour
{
    public AudioClip fireSound;
    public float cooldown = 0.25f;
    public float recoil = 0.05f;

    float nextShot;
    float flashUntil;
    Vector3 restPos;
    Light flash;

    void Start()
    {
        restPos = transform.localPosition;

        var go = new GameObject("MuzzleFlash");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.12f, 0.2f);
        flash = go.AddComponent<Light>();
        flash.type = LightType.Point;
        flash.color = new Color(1f, 0.7f, 0.3f);
        flash.range = 6f;
        flash.intensity = 4f;
        flash.enabled = false;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= nextShot)
        {
            nextShot = Time.time + cooldown;
            Fire();
        }

        if (flash.enabled && Time.time > flashUntil)
            flash.enabled = false;

        transform.localPosition = Vector3.Lerp(transform.localPosition, restPos, Time.deltaTime * 15f);
    }

    void Fire()
    {
        if (fireSound != null)
            AudioSource.PlayClipAtPoint(fireSound, transform.position);

        flash.enabled = true;
        flashUntil = Time.time + 0.05f;
        transform.localPosition = restPos - new Vector3(0f, 0f, recoil);
    }
}