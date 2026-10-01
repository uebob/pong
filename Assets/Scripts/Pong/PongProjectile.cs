using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Proyectil teledirigido (Rigidbody, Unity 6) con:
/// - Estadísticas resueltas (definición + pasivos del dueño) en Stats.
/// - Behaviors modulares (innatos + concedidos por pasivos).
/// - Owner + Faction: quién lo lanzó. Se fijan al crearlo y NO cambian nunca
///   (parrearlo no lo cambia de dueño). Solo el dueño puede guardarlo.
/// - Target propio: lo elige el propio pong. Al lanzarse según su facción; tras un parry,
///   al terminar la recuperación: enemigo más cercano o, si no hay, el jugador.
/// - Estados: Flying, HitStop, InHolster (desactivado y guardado en la cola).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PongProjectile : MonoBehaviour
{
    public enum State { Flying, HitStop, InHolster }

    [Header("Datos (los rellena PongDefinition.Spawn)")]
    [SerializeField] private PongDefinition definition;
    [Tooltip("Quien lo lanza. Se fija al crearlo y no cambia nunca.")]
    [SerializeField] private GameObject owner;
    [SerializeField] public PongFaction faction = PongFaction.Enemy;

    [Header("Objetivo (lo elige el propio pong)")]
    [SerializeField] private Transform target;

    [Header("Colisiones")]
    [Tooltip("Rebota contra lo que golpee. El daño lo hacen los behaviors.")]
    [SerializeField] private bool bounceOnCollision = true;

    public event Action<PongProjectile> Launched;
    public event Action<PongProjectile, int> Parried;
    public event Action<PongProjectile, Collision> Hit;
    public event Action<PongProjectile> Stored;
    public event Action<PongProjectile, Transform> TargetChanged;

    public PongDefinition Definition => definition;
    public PongStatSheet Stats { get; private set; }
    public GameObject Owner => owner;
    public PongFaction Faction => faction;
    public Transform Target => target;
    public State CurrentState => state;
    public int ParryCount => parryCount;
    public float CurrentSpeed => speed;
    public bool IsConfigured => Stats != null;

    /// <summary>True mientras vuela sin objetivo tras un parry (antes de volver a elegir).</summary>
    public bool IsRecovering => recovering;

    public bool CanBeParried =>
        IsConfigured && state == State.Flying &&
        Time.time - lastParryTime >= Settings.parryLockout;

    /// <summary>Solo el dueño (jugador) puede guardar el pong, y solo si está volando.</summary>
    public bool CanBeStoredBy(GameObject who) =>
        IsConfigured && state == State.Flying &&
        who != null && owner == who;

    private ProjectileSettings Settings => definition.settings;

    private float CruiseSpeed
    {
        get
        {
            float v = Stats.Get(PongStat.CruiseSpeed) +
                      Stats.Get(PongStat.SpeedGainPerParry) * parryCount;
            float max = Stats.Get(PongStat.MaxCruiseSpeed);
            return max > 0f ? Mathf.Min(v, max) : v;
        }
    }

    private Rigidbody rb;
    private State state = State.Flying;
    private bool hasLaunched;

    private Vector3 direction;
    private float speed;

    // homing / recuperación
    private bool recovering;
    private float timeSinceParry = 999f;
    private float timeSinceRetarget = 999f;
    private float homingWeight = 1f;
    private float nextAcquireTime;

    private float hitStopTimer;
    private Vector3 pendingDirection;

    private float lastParryTime = -999f;
    private int parryCount;

    private readonly List<PongBehavior> behaviors = new List<PongBehavior>();
    private readonly Dictionary<PongBehavior, object> behaviorState = new Dictionary<PongBehavior, object>();

    // =====================================================================
    // Ciclo de vida
    // =====================================================================

    private void Reset()
    {
        ConfigureRigidbody();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ConfigureRigidbody();

        direction = transform.forward;

        // Para prefabs colocados a mano en escena con una definición ya asignada.
        if (definition != null)
        {
            RebuildLoadout();
            speed = CruiseSpeed;
        }
    }

    private void Start()
    {
        // Un pong colocado a mano en escena se comporta como si lo hubieran lanzado.
        if (!hasLaunched && IsConfigured && state == State.Flying)
            Launch(transform.position, transform.forward);
    }

    private void ConfigureRigidbody()
    {
        var body = GetComponent<Rigidbody>();
        body.useGravity = false;
        body.linearDamping = 0f;
        body.angularDamping = 0f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.constraints = RigidbodyConstraints.FreezeRotation;
    }

    /// <summary>
    /// Llamado por PongDefinition.Spawn justo tras instanciar. Fija el owner y la
    /// facción de forma permanente.
    /// </summary>
    public void Setup(PongDefinition newDefinition, GameObject newOwner, PongFaction newFaction)
    {
        definition = newDefinition;
        owner = newOwner;
        faction = newFaction;
        RebuildLoadout();

        direction = transform.forward;
        speed = CruiseSpeed;
    }

    /// <summary>
    /// Recalcula estadísticas y behaviors a partir de la definición y los pasivos del owner.
    /// Se llama al crear y al lanzar; llámalo a mano si quieres que un pasivo recién
    /// recogido afecte a un pong que ya está volando.
    /// </summary>
    public void RebuildLoadout()
    {
        if (definition == null)
        {
            Debug.LogError("PongProjectile sin PongDefinition.", this);
            return;
        }

        IReadOnlyList<PassiveItem> passives = null;
        if (owner != null && owner.TryGetComponent<IPongModifierProvider>(out var provider))
            passives = provider.ActivePassives;

        Stats = PongStatSheet.Build(definition, passives);

        behaviors.Clear();
        behaviorState.Clear();
        AddBehaviors(definition.innateBehaviors);

        if (passives != null)
            foreach (var passive in passives)
                if (passive != null) AddBehaviors(passive.grantedBehaviors);
    }

    private void AddBehaviors(List<PongBehavior> source)
    {
        if (source == null) return;
        foreach (var b in source)
            if (b != null && !behaviors.Contains(b))
                behaviors.Add(b);
    }

    /// <summary>Estado propio de un behavior para ESTE proyectil (se crea al pedirlo).</summary>
    public T GetState<T>(PongBehavior behavior) where T : class, new()
    {
        if (behaviorState.TryGetValue(behavior, out object existing))
            return (T)existing;

        var created = new T();
        behaviorState[behavior] = created;
        return created;
    }

    /// <summary>Fuerza un objetivo. Se sobrescribe al volver a elegir (fin de recuperación).</summary>
    public void SetTarget(Transform newTarget) => ChangeTarget(newTarget);

    public void Despawn() => Destroy(gameObject);

    // =====================================================================
    // Objetivo
    // =====================================================================

    private void ChangeTarget(Transform newTarget)
    {
        if (target == newTarget) return;
        target = newTarget;
        TargetChanged?.Invoke(this, target);
    }

    /// <summary>
    /// Regla general: enemigo más cercano; si no hay ninguno, el jugador.
    /// Excepción (solo al lanzar): un pong enemigo apunta primero al jugador.
    /// </summary>
    private void AcquireTarget(bool initial)
    {
        Vector3 from = rb != null ? rb.position : transform.position;
        Transform found = null;

        if (faction == PongFaction.Enemy)
            found = PongTargeting.FindPlayer();

        if (found == null)
            found = PongTargeting.FindDefaultTarget(from);

        ChangeTarget(found);
    }

    public void ChangeFaction(PongFaction newFaction)
    {
        faction = newFaction;
    }

    // =====================================================================
    // Guardar y lanzar (cola del jugador)
    // =====================================================================

    /// <summary>Lo desactiva y lo cuelga del holster. Pierde parries y velocidad.</summary>
    public void Store(Transform holster)
    {
        if (!IsConfigured) return;

        for (int i = 0; i < behaviors.Count; i++)
            behaviors[i].OnStored(this);
        Stored?.Invoke(this);

        state = State.InHolster;
        parryCount = 0;
        speed = 0f;
        recovering = false;
        ChangeTarget(null);

        rb.linearVelocity = Vector3.zero;
        transform.SetParent(holster, false);
        transform.localPosition = Vector3.zero;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Lanza el pong (también sirve para uno recién instanciado). Sale a velocidad de
    /// crucero, con los parries a 0; las estadísticas se resuelven de nuevo.
    /// Si no se pasa initialTarget, lo elige el propio pong (ver AcquireTarget).
    /// </summary>
    public void Launch(Vector3 position, Vector3 launchDirection, Transform initialTarget = null)
    {
        hasLaunched = true;

        if (state == State.InHolster)
        {
            transform.SetParent(null, true);
            gameObject.SetActive(true);
        }

        RebuildLoadout();

        parryCount = 0;
        recovering = false;
        homingWeight = 1f;
        timeSinceParry = 999f;
        timeSinceRetarget = 999f;
        lastParryTime = -999f;
        nextAcquireTime = 0f;

        direction = launchDirection.sqrMagnitude > 1e-4f ? launchDirection.normalized : transform.forward;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        transform.SetPositionAndRotation(position, rotation);
        rb.position = position;
        rb.rotation = rotation;

        speed = CruiseSpeed;
        state = State.Flying;

        if (initialTarget != null) ChangeTarget(initialTarget);
        else AcquireTarget(initial: true);

        ApplyMotion();

        Launched?.Invoke(this);
        for (int i = 0; i < behaviors.Count; i++)
            behaviors[i].OnLaunched(this);
    }

    // =====================================================================
    // Parry
    // =====================================================================

    /// <summary>
    /// Parrea el proyectil hacia aimDirection. No cambia su owner. Pierde el objetivo
    /// hasta que acabe la recuperación; entonces elige uno nuevo.
    /// </summary>
    public bool Parry(Vector3 aimDirection)
    {
        if (!CanBeParried) return false;

        pendingDirection = aimDirection.sqrMagnitude > 1e-4f ? aimDirection.normalized : -direction;

        parryCount++;
        lastParryTime = Time.time;

        // Sin objetivo durante la recuperación: si no, el homing volvería hacia quien lo lanzó.
        recovering = true;
        ChangeTarget(null);

        state = State.HitStop;
        hitStopTimer = Settings.hitStopDuration;
        rb.linearVelocity = Vector3.zero;

        Parried?.Invoke(this, parryCount);
        for (int i = 0; i < behaviors.Count; i++)
            behaviors[i].OnParried(this, parryCount);

        return true;
    }

    // =====================================================================
    // Simulación
    // =====================================================================

    private void FixedUpdate()
    {
        if (!IsConfigured || state == State.InHolster) return;

        float dt = Time.fixedDeltaTime;

        if (state == State.HitStop)
        {
            rb.linearVelocity = Vector3.zero;
            hitStopTimer -= dt;
            if (hitStopTimer <= 0f) ReleaseFromHitStop();
            return;
        }

        UpdateTargetingAndHoming(dt);
        Steer(dt);
        ApplyMotion();

        for (int i = 0; i < behaviors.Count; i++)
            behaviors[i].Tick(this, dt);
    }

    private void ReleaseFromHitStop()
    {
        direction = pendingDirection;
        speed = CruiseSpeed * Stats.Get(PongStat.ParryBoostMultiplier);

        // Empieza la recuperación: vuela hacia fuera sin homing y sin objetivo.
        recovering = true;
        timeSinceParry = 0f;
        homingWeight = 0f;

        state = State.Flying;
        ApplyMotion();
    }

    private void UpdateTargetingAndHoming(float dt)
    {
        if (recovering)
        {
            timeSinceParry += dt;
            homingWeight = 0f;

            // Fin de la recuperación: se elige objetivo (enemigo más cercano, o el jugador).
            if (timeSinceParry >= Settings.homingRecoveryTime)
            {
                recovering = false;
                timeSinceRetarget = 0f;
                AcquireTarget(initial: false);
            }
            return;
        }

        // Tras elegir objetivo, el homing entra de forma gradual.
        timeSinceRetarget += dt;
        float t = Settings.homingRampTime > 0f
            ? Mathf.Clamp01(timeSinceRetarget / Settings.homingRampTime)
            : 1f;
        homingWeight = Settings.homingRampCurve.Evaluate(t);

        // Objetivo perdido (p. ej. el enemigo murió): buscar otro, sin saturar la CPU.
        if (target == null && Time.time >= nextAcquireTime)
        {
            nextAcquireTime = Time.time + Settings.retargetInterval;
            AcquireTarget(initial: false);
        }
    }

    private void Steer(float dt)
    {
        float slowdown = 1f;

        if (target != null)
        {
            Vector3 toTarget = target.position - rb.position;
            float distance = toTarget.magnitude;

            if (distance > 0.001f)
            {
                Vector3 desired = toTarget / distance;
                float alignment = Vector3.Dot(direction, desired);

                float turn = Stats.Get(PongStat.TurnRate) * homingWeight;
                if (distance < Settings.closeRange)
                    turn *= Settings.closeRangeTurnMultiplier;

                direction = Vector3.RotateTowards(
                    direction, desired, turn * Mathf.Deg2Rad * dt, 0f).normalized;

                // Frenado suave según alineación, atenuado mientras el homing entra.
                float curveFactor = Settings.slowdownByAlignment.Evaluate(alignment);
                slowdown = Mathf.Lerp(1f, curveFactor, homingWeight);
            }
        }

        float targetSpeed = CruiseSpeed * slowdown;

        // Suavizado independiente del framerate.
        float k = 1f - Mathf.Exp(-Settings.speedSmoothing * dt);
        speed = Mathf.Lerp(speed, targetSpeed, k);
    }

    private void ApplyMotion()
    {
        rb.linearVelocity = direction * speed;
        rb.MoveRotation(Quaternion.LookRotation(direction, Vector3.up));
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsConfigured || state == State.InHolster) return;

        Hit?.Invoke(this, collision);
        for (int i = 0; i < behaviors.Count; i++)
            behaviors[i].OnHit(this, collision);

        if (bounceOnCollision && collision.contactCount > 0)
        {
            direction = Vector3.Reflect(direction, collision.GetContact(0).normal).normalized;
            ApplyMotion();
        }
    }
}