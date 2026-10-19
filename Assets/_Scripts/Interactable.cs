using UnityEngine;

[RequireComponent(typeof(Collider))]
/// <summary>
/// Clase base de todo objeto con el que el jugador puede interactuar con la tecla E
/// (puertas, llaves, notas, brujula...). Cada hijo define que pasa en <see cref="Interact"/>.
/// </summary>
public abstract class Interactable : MonoBehaviour
{
    /// <summary>Texto que se muestra al apuntar al objeto.</summary>
    public string prompt = "Pulsa E para interactuar";
    /// <summary>Sonido opcional que se reproduce al interactuar.</summary>
    public AudioClip sound;

    /// <summary>Indica si el objeto puede usarse en este momento.</summary>
    public virtual bool CanInteract => true;

    /// <summary>Accion al interactuar. La define cada clase hija.</summary>
    /// <param name="player">Jugador que interactua.</param>
    public abstract void Interact(PlayerController player);

    protected void PlaySound()
    {
        if (sound != null)
            AudioSource.PlayClipAtPoint(sound, transform.position);
    }
}