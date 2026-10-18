using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class Interactable : MonoBehaviour
{
    public string prompt = "Pulsa E para interactuar";
    public AudioClip sound;

    public virtual bool CanInteract => true;

    public abstract void Interact(PlayerController player);

    protected void PlaySound()
    {
        if (sound != null)
            AudioSource.PlayClipAtPoint(sound, transform.position);
    }
}