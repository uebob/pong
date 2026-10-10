using System;
using UnityEngine;
using UnityEngine.SceneManagement;
/// <summary>
/// Vida común de jugador y enemigos. Implementa IPongDamageable, así que los behaviors de los
/// pongs (DamageOnHit, ExplodeOnHit...) la dañan sin saber qué clase concreta es.
/// No filtra por bando: cualquier pong daña a cualquiera (incluido el propio jugador).
/// Las subclases pueden cambiar CUÁNDO se aplica el daño sobrescribiendo TakeDamage(amount, source).
/// </summary>
public class Health : MonoBehaviour, IPongDamageable
{
    public float health;
    public float maxHealth = 100f;

    /// <summary>Daño realmente aplicado (para HUD, flash de pantalla, sonido...).</summary>
    public event Action<float> Damaged;
    public event Action Died;

    public bool IsDead { get; private set; }

    protected virtual void Start()
    {
        health = maxHealth;
    }

    /// <summary>Daño directo, sin pong de por medio.</summary>
    public void TakeDamage(float damage) => ApplyDamage(damage);

    /// <summary>Punto de entrada de los pongs (IPongDamageable).</summary>
    public virtual void TakeDamage(float amount, PongProjectile source) => ApplyDamage(amount);

    protected virtual void ApplyDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        health -= amount;
        Damaged?.Invoke(amount);

        if (health <= 0f)
        {
            IsDead = true;
            Die();
        }
    }

    protected virtual void Die()
    {
        Died?.Invoke();
        if(gameObject.CompareTag("Player"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }
        Destroy(gameObject);
    }
}
