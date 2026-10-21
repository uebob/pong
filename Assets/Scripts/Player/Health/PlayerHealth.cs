using System.Collections;
using UnityEngine;

/// <summary>
/// Vida del jugador. Recibe daño de cualquier pong, también de los suyos.
/// El daño (y los efectos de estado) de un pong se aplica tras un periodo de gracia: si en ese tiempo
/// el pong se parrea (o se guarda), no hay daño ni efecto. Da margen para parrear "justo tarde".
/// Tras recibir el golpe, el pong se intenta guardar en la cola.
/// Tras recibir daño (de un pong o directo, p. ej. contacto) hay un periodo de invulnerabilidad.
/// </summary>
public class PlayerHealth : Health
{
    [Tooltip("Tiempo (en tiempo real) tras el golpe durante el cual un parry aún cancela el daño. 0 = daño inmediato.")]
    [SerializeField] private float collisionGracePeriod = 0.1f;

    [Header("Invulnerabilidad (iframes)")]
    [Tooltip("Segundos de invulnerabilidad tras recibir daño. 0 = desactivado.")]
    [SerializeField] private float invulnerabilityTime = 1f;

    private PongInventory inventory;
    private float invulnerableUntil;

    /// <summary>True mientras el jugador no puede recibir daño (para parpadeo, sonido, etc.).</summary>
    public bool IsInvulnerable => Time.time < invulnerableUntil;

    protected override void Start()
    {
        base.Start();
        inventory = GetComponent<PongInventory>();
    }

    /// <summary>
    /// Concede invulnerabilidad extra (p. ej. durante un dash). Nunca acorta la que ya hay.
    /// </summary>
    public void GrantInvulnerability(float seconds)
    {
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);
    }

    /// <summary>
    /// Punto por el que pasa el daño de pongs y daño directo: aquí se aplican los iframes.
    /// (El daño de efectos de estado, como la quemadura, no pasa por aquí: ver Health.TakeStatusDamage.)
    /// </summary>
    protected override void ApplyDamage(float amount)
    {
        if (IsInvulnerable) return;

        float before = health;
        base.ApplyDamage(amount);

        // Los iframes solo empiezan si el golpe hizo daño de verdad: no si lo bloqueó, por ejemplo,
        // una Invincibility o una Resistance total.
        if (!IsDead && health < before && invulnerabilityTime > 0f)
            GrantInvulnerability(invulnerabilityTime);
    }

    public override void TakeDamage(float amount, PongProjectile source)
    {
        // Sin pong de por medio, o sin gracia: inmediato.
        if (source == null || collisionGracePeriod <= 0f)
        {
            Resolve(amount, source);
            return;
        }

        StartCoroutine(ResolveAfterGrace(amount, source));
    }

    /// <summary>Los efectos de estado de un pong esperan el mismo margen de parry que el daño.</summary>
    public override void ReceiveStatus(StatusEffectSpec spec, PongProjectile source)
    {
        if (source == null || collisionGracePeriod <= 0f)
        {
            ApplyStatus(spec);
            return;
        }

        StartCoroutine(StatusAfterGrace(spec, source));
    }

    private IEnumerator ResolveAfterGrace(float amount, PongProjectile source)
    {
        // Un parry incrementa ParryCount: comparar con el valor del golpe evita referencias caducadas.
        int parriesAtHit = source.ParryCount;

        yield return new WaitForSecondsRealtime(collisionGracePeriod);

        if (HitWasCancelled(source, parriesAtHit)) yield break;

        Resolve(amount, source);
    }

    private IEnumerator StatusAfterGrace(StatusEffectSpec spec, PongProjectile source)
    {
        int parriesAtHit = source.ParryCount;

        yield return new WaitForSecondsRealtime(collisionGracePeriod);

        if (HitWasCancelled(source, parriesAtHit)) yield break;

        ApplyStatus(spec);
    }

    /// <summary>
    /// ¿Se parreó o se guardó el pong durante la gracia? Si el pong ya no existe (p. ej. explotó al
    /// golpearte), el golpe era inevitable y no cuenta como cancelado.
    /// </summary>
    private static bool HitWasCancelled(PongProjectile source, int parriesAtHit)
    {
        if (source == null) return false;

        return source.ParryCount != parriesAtHit ||
               source.CurrentState == PongProjectile.State.InHolster;
    }

    private void Resolve(float amount, PongProjectile source)
    {
        ApplyDamage(amount); // si hay iframes, no hace daño

        // El pong se guarda igualmente, haya hecho daño o no
        if (!IsDead && source != null && inventory != null)
            inventory.TryStore(source);
    }
}