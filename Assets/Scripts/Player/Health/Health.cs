using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Vida común de jugador y enemigos. Implementa IPongDamageable, así que los behaviors de los
/// pongs (DamageOnHit, ExplodeOnHit...) la dañan sin saber qué clase concreta es.
/// No filtra por bando: cualquier pong daña a cualquiera (incluido el propio jugador).
/// Las subclases pueden cambiar CUÁNDO se aplica el daño sobrescribiendo TakeDamage(amount, source),
/// y QUÉ daño llega a aplicarse (p. ej. iframes) sobrescribiendo ApplyDamage(amount).
///
/// Efectos de estado (StatusEffects, opcional): la invencibilidad bloquea el daño y los multiplicadores
/// de Weakness/Resistance lo modifican, ambos en ApplyDamageCore.
/// </summary>
public class Health : MonoBehaviour, IPongDamageable
{
    public float health;
    public float maxHealth = 100f;

    /// <summary>Daño realmente aplicado (para HUD, flash de pantalla, sonido...).</summary>
    public event Action<float> Damaged;
    /// <summary>Vida realmente recuperada (ya limitada por maxHealth).</summary>
    public event Action<float> Healed;
    public event Action Died;

    public bool IsDead { get; private set; }

    /// <summary>Efectos de estado de esta entidad (null si no tiene).</summary>
    public StatusEffects Status { get; private set; }

    protected virtual void Awake()
    {
        Status = GetComponentInParent<StatusEffects>();
    }

    protected virtual void Start()
    {
        health = maxHealth;
    }

    /// <summary>Daño directo, sin pong de por medio.</summary>
    public void TakeDamage(float damage) => ApplyDamage(damage);

    /// <summary>Punto de entrada de los pongs (IPongDamageable).</summary>
    public virtual void TakeDamage(float amount, PongProjectile source) => ApplyDamage(amount);

    /// <summary>
    /// Daño de un efecto de estado (quemadura). Respeta invencibilidad y defensa, pero se salta los
    /// iframes de las subclases (ApplyDamage): si no, cada tick los activaría y bloquearía otros golpes.
    /// </summary>
    public void TakeStatusDamage(float amount) => ApplyDamageCore(amount);

    /// <summary>
    /// Punto de entrada para efectos de estado que traen los pongs (ApplyStatusOnHitBehavior).
    /// Virtual para que las subclases decidan CUÁNDO se aplican (el jugador espera su margen de parry).
    /// </summary>
    public virtual void ReceiveStatus(StatusEffectSpec spec, PongProjectile source) => ApplyStatus(spec);

    /// <summary>Aplica el efecto ya, sin esperas. Úsalo desde subclases (también dentro de corrutinas).</summary>
    protected void ApplyStatus(StatusEffectSpec spec)
    {
        if (Status != null) Status.Apply(spec);
    }

    /// <summary>Recupera vida sin pasar de maxHealth. No hace nada si está muerto.</summary>
    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;

        float before = health;
        health = Mathf.Min(health + amount, maxHealth);

        float healed = health - before;
        if (healed > 0f) Healed?.Invoke(healed);
    }

    /// <summary>Punto por el que pasa el daño de pongs y daño directo. Virtual para poder filtrarlo en subclases.</summary>
    protected virtual void ApplyDamage(float amount) => ApplyDamageCore(amount);

    /// <summary>El daño en sí: invencibilidad, multiplicador de defensa, vida, eventos y muerte.</summary>
    protected void ApplyDamageCore(float amount)
    {
        if (IsDead || amount <= 0f) return;

        if (Status != null)
        {
            if (Status.IsInvincible) return;

            amount *= Status.DamageTakenMultiplier;
            if (amount <= 0f) return;
        }

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
        if (gameObject.CompareTag("Player"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }
        Destroy(gameObject);
    }
}