using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Efectos de estado (bufos y debufos) de una entidad. Ponlo en el jugador y en cada enemigo.
/// Es el ÚNICO punto de entrada: Apply(tipo, magnitud, duración). Lo llaman los pongs
/// (ApplyStatusOnHitBehavior), items, trampas...
///
/// - Apilado sin límite: cada Apply crea una instancia independiente con su propia duración.
///   Los multiplicadores se multiplican entre sí, y al caducar una instancia solo desaparece su parte.
/// - Burn y Regeneration actúan por ticks (tickInterval) por instancia: una de 3 s da 3 ticks.
/// - Invincibility: mientras dura, los efectos DAÑINOS (Slowness, Weakness, Freeze, Burn) quedan latentes:
///   no hacen nada y, por defecto, su temporizador se pausa. Al terminar la invencibilidad vuelven.
/// - Freeze: bloqueo total. Desactiva los componentes de la lista "Disable While Frozen" (movimiento,
///   parry, lanzar pongs...) y bloquea el Rigidbody.
/// - Al morir, todos los efectos desaparecen.
///
/// Los demás scripts solo CONSULTAN: SpeedMultiplier, DamageTakenMultiplier, IsFrozen, IsInvincible.
/// </summary>
public class StatusEffects : MonoBehaviour
{
    [Header("Ticks")]
    [Tooltip("Cada cuántos segundos actúan Burn (daño) y Regeneration (curación).")]
    [SerializeField] private float tickInterval = 1f;

    [Header("Invencibilidad")]
    [Tooltip("Si es true, los efectos dañinos pausan su temporizador mientras haya Invincibility " +
             "y reanudan después. Si es false, siguen contando (y pueden caducar durante la invencibilidad).")]
    [SerializeField] private bool pauseHarmfulWhileInvincible = true;

    [Header("Congelación")]
    [Tooltip("Componentes que se desactivan mientras está congelado. Jugador: PlayerMovement, PlayerParry, " +
             "PongInventory. Enemigo: EnemyFollowPlayer, EnemyHover... Solo se reactivan los que desactivó este script.")]
    [SerializeField] private MonoBehaviour[] disableWhileFrozen;
    [Tooltip("Bloquea también el Rigidbody (posición y rotación) mientras está congelado.")]
    [SerializeField] private bool freezeRigidbody = true;

    [Header("Pruebas")]
    [SerializeField] private StatusEffectSpec debugEffect = new StatusEffectSpec(StatusEffectType.Slowness, 0.3f, 3f);

    /// <summary>Se aplicó un efecto nuevo.</summary>
    public event Action<StatusEffectSpec> Applied;
    /// <summary>Caducó (o se quitó) una instancia de este tipo.</summary>
    public event Action<StatusEffectType> Expired;
    /// <summary>Cambió el conjunto de efectos (para HUD, partículas...).</summary>
    public event Action Changed;

    /// <summary>Multiplicador de velocidad: producto de Slowness (1 - m) y Haste (1 + m).</summary>
    public float SpeedMultiplier { get; private set; } = 1f;

    /// <summary>Multiplicador de daño recibido: producto de Weakness (1 + m) y Resistance (1 - m).</summary>
    public float DamageTakenMultiplier { get; private set; } = 1f;

    public bool IsFrozen { get; private set; }
    public bool IsInvincible { get; private set; }

    public int ActiveCount => active.Count;

    private class ActiveEffect
    {
        public StatusEffectType type;
        public float magnitude;
        public float duration;
        public float elapsed;
        public int ticksDone;
        public int maxTicks;
    }

    private readonly List<ActiveEffect> active = new List<ActiveEffect>();
    private readonly List<StatusEffectType> expiredBuffer = new List<StatusEffectType>();
    private readonly List<MonoBehaviour> disabledByUs = new List<MonoBehaviour>();

    private Health health;
    private Rigidbody body;
    private RigidbodyConstraints savedConstraints;
    private bool rigidbodyFrozen;

    private bool updating;
    private bool clearPending;

    private void Awake()
    {
        health = GetComponentInParent<Health>();
        body = GetComponentInParent<Rigidbody>();
    }

    private void OnEnable()
    {
        if (health != null)
            health.Died += OnDied;
    }

    private void OnDisable()
    {
        if (health != null)
            health.Died -= OnDied;

        // Si se desactiva en pleno congelado, no dejamos componentes desactivados ni el Rigidbody bloqueado.
        if (IsFrozen)
        {
            SetFrozenState(false);
            IsFrozen = false;
        }
    }

    // =====================================================================
    // API pública
    // =====================================================================

    /// <summary>
    /// Aplica un efecto. Cada llamada se apila con las anteriores (sin límite).
    /// La magnitud se limita a valores válidos (Slowness y Resistance: 0 a 1; el resto: ≥ 0).
    /// </summary>
    public void Apply(StatusEffectType type, float magnitude, float duration)
    {
        if (duration <= 0f) return;
        if (health != null && health.IsDead) return;

        var effect = new ActiveEffect
        {
            type = type,
            magnitude = SanitizeMagnitude(type, magnitude),
            duration = duration,
            maxTicks = StatusEffectInfo.UsesTicks(type)
                ? Mathf.FloorToInt(duration / Mathf.Max(0.01f, tickInterval) + 0.0001f)
                : 0
        };

        active.Add(effect);
        Recalculate();

        Applied?.Invoke(new StatusEffectSpec(type, effect.magnitude, duration));
    }

    public void Apply(StatusEffectSpec spec) => Apply(spec.type, spec.magnitude, spec.duration);

    /// <summary>¿Hay alguna instancia de este tipo (aunque esté latente por una invencibilidad)?</summary>
    public bool Has(StatusEffectType type)
    {
        for (int i = 0; i < active.Count; i++)
            if (active[i].type == type) return true;
        return false;
    }

    /// <summary>Cuántas instancias de este tipo hay apiladas.</summary>
    public int GetStacks(StatusEffectType type)
    {
        int count = 0;
        for (int i = 0; i < active.Count; i++)
            if (active[i].type == type) count++;
        return count;
    }

    /// <summary>Quita todas las instancias de un tipo (p. ej. un item que cura la quemadura).</summary>
    public void Clear(StatusEffectType type)
    {
        bool removed = false;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (active[i].type != type) continue;
            active.RemoveAt(i);
            removed = true;
        }

        if (!removed) return;

        Recalculate();
        Expired?.Invoke(type);
    }

    public void ClearAll()
    {
        if (active.Count == 0) return;

        expiredBuffer.Clear();
        foreach (var e in active)
            if (!expiredBuffer.Contains(e.type)) expiredBuffer.Add(e.type);

        active.Clear();
        Recalculate();

        foreach (var type in expiredBuffer)
            Expired?.Invoke(type);
    }

    [ContextMenu("Apply Debug Effect")]
    private void ApplyDebugEffect() => Apply(debugEffect);

    // =====================================================================
    // Simulación
    // =====================================================================

    private void Update()
    {
        if (active.Count == 0) return;

        float dt = Time.deltaTime;
        expiredBuffer.Clear();
        updating = true;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            // Latente por invencibilidad: no cuenta mientras tanto (si así se ha configurado).
            if (pauseHarmfulWhileInvincible && IsDormant(active[i].type)) continue;

            ActiveEffect e = active[i];
            e.elapsed += dt;
            ProcessTicks(e);

            // Un tick pudo matar a la entidad (OnDied pide limpiar todo): se hace fuera del bucle.
            if (clearPending) break;

            if (e.elapsed >= e.duration)
            {
                expiredBuffer.Add(e.type);
                active.RemoveAt(i);
            }
        }

        updating = false;

        if (clearPending)
        {
            clearPending = false;
            ClearAll();
            return;
        }

        if (expiredBuffer.Count > 0)
        {
            Recalculate();
            foreach (var type in expiredBuffer)
                Expired?.Invoke(type);
        }
    }

    private void ProcessTicks(ActiveEffect e)
    {
        if (e.maxTicks <= 0) return;

        float interval = Mathf.Max(0.01f, tickInterval);

        while (e.ticksDone < e.maxTicks && e.elapsed + 0.0001f >= (e.ticksDone + 1) * interval)
        {
            e.ticksDone++;
            DoTick(e, interval);

            if (clearPending) return;
        }
    }

    private void DoTick(ActiveEffect e, float interval)
    {
        if (health == null) return;

        // magnitud = por segundo; cada tick reparte lo que corresponde a su intervalo.
        float amount = e.magnitude * interval;

        if (e.type == StatusEffectType.Burn)
            health.TakeStatusDamage(amount);   // respeta defensa e invencibilidad; no activa iframes
        else if (e.type == StatusEffectType.Regeneration)
            health.Heal(amount);
    }

    private void OnDied()
    {
        if (updating) clearPending = true;
        else ClearAll();
    }

    // =====================================================================
    // Resolución de estado
    // =====================================================================

    /// <summary>Un efecto dañino está latente mientras hay Invincibility.</summary>
    private bool IsDormant(StatusEffectType type) =>
        IsInvincible && StatusEffectInfo.IsHarmful(type);

    private void Recalculate()
    {
        bool wasFrozen = IsFrozen;

        IsInvincible = Has(StatusEffectType.Invincibility);

        float speed = 1f;
        float damageTaken = 1f;
        bool frozen = false;

        for (int i = 0; i < active.Count; i++)
        {
            ActiveEffect e = active[i];
            if (IsDormant(e.type)) continue;

            switch (e.type)
            {
                case StatusEffectType.Slowness:   speed *= 1f - e.magnitude; break;
                case StatusEffectType.Haste:      speed *= 1f + e.magnitude; break;
                case StatusEffectType.Weakness:   damageTaken *= 1f + e.magnitude; break;
                case StatusEffectType.Resistance: damageTaken *= 1f - e.magnitude; break;
                case StatusEffectType.Freeze:     frozen = true; break;
            }
        }

        SpeedMultiplier = speed;
        DamageTakenMultiplier = damageTaken;
        IsFrozen = frozen;

        if (frozen != wasFrozen)
            SetFrozenState(frozen);

        Changed?.Invoke();
    }

    private static float SanitizeMagnitude(StatusEffectType type, float magnitude)
    {
        switch (type)
        {
            case StatusEffectType.Slowness:
            case StatusEffectType.Resistance:
                return Mathf.Clamp01(magnitude);   // 1 = -100 %: más no tiene sentido (sería velocidad o daño negativos)
            case StatusEffectType.Freeze:
            case StatusEffectType.Invincibility:
                return 0f;                          // no usan magnitud
            default:
                return Mathf.Max(0f, magnitude);
        }
    }

    // =====================================================================
    // Congelación
    // =====================================================================

    private void SetFrozenState(bool frozen)
    {
        if (frozen)
        {
            disabledByUs.Clear();
            if (disableWhileFrozen != null)
            {
                foreach (MonoBehaviour component in disableWhileFrozen)
                {
                    if (component == null || !component.enabled) continue;
                    component.enabled = false;
                    disabledByUs.Add(component);
                }
            }

            if (freezeRigidbody && body != null && !body.isKinematic && !rigidbodyFrozen)
            {
                savedConstraints = body.constraints;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.constraints = RigidbodyConstraints.FreezeAll;
                rigidbodyFrozen = true;
            }
        }
        else
        {
            // Primero el Rigidbody, luego los componentes: así no arrancan con la velocidad de antes.
            if (rigidbodyFrozen && body != null)
            {
                body.constraints = savedConstraints;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            rigidbodyFrozen = false;

            foreach (MonoBehaviour component in disabledByUs)
                if (component != null) component.enabled = true;
            disabledByUs.Clear();
        }
    }
}
