using UnityEngine;
using UnityEngine.Events;

// Poner en cualquier objeto que deba recibir dano (criatura, objetivo de prueba...).
// El objeto necesita un Collider para que las balas lo detecten.
/// <summary>
/// Vida de cualquier objeto que pueda recibir dano (criaturas, objetivos de prueba).
/// Necesita un Collider para que las balas de la pistola lo detecten.
/// </summary>
public class Health : MonoBehaviour
{
    /// <summary>Vida maxima al iniciar.</summary>
    public float maxHealth = 100f;
    /// <summary>Si es verdadero, el objeto se destruye cuando su vida llega a cero.</summary>
    public bool destroyOnDeath = true;
    /// <summary>Evento que se dispara al morir (para reproducir sonidos, abrir puertas, etc.).</summary>
    public UnityEvent onDeath;

    /// <summary>Vida actual.</summary>
    public float CurrentHealth { get; private set; }
    bool dead;

    void Awake() { CurrentHealth = maxHealth; }

    // Aviso de cada golpe recibido (lo usa Enemy, que no depende de la barra de vida)
    /// <summary>Aviso de cada golpe recibido; entrega la cantidad de dano.</summary>
    public event System.Action<float> Damaged;

    /// <summary>Resta vida. Si llega a cero, el objeto muere.</summary>
    /// <param name="amount">Cantidad de dano.</param>
    public void TakeDamage(float amount)
    {
        if (dead) return;
        Damaged?.Invoke(amount);
        CurrentHealth -= amount;
        if (CurrentHealth <= 0f) Die();
    }

    void Die()
    {
        dead = true;
        onDeath?.Invoke();
        if (destroyOnDeath) Destroy(gameObject);
    }
}