using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PongProjectile : MonoBehaviour
{
    public enum State { Homing, HitStop }

    [Header("Referencias")]
    [SerializeField] private ProjectileSettings settings;
    [SerializeField] private Transform target;

    [Header("Colisiones")]
    [Tooltip("Si es true, rebota contra lo que golpee. Usa el evento Hit para daño/destrucción.")]
    [SerializeField] private bool bounceOnCollision = true;

    /// <summary>Se dispara al parrear. Parámetros: proyectil, nº total de parries.</summary>
    public event Action<PongProjectile, int> Parried;

    /// <summary>Se dispara al colisionar con algo.</summary>
    public event Action<PongProjectile, Collision> Hit;

    public Transform Target => target;
    public State CurrentState => state;
    public int ParryCount => parryCount;
    public float CurrentSpeed => speed;

    public bool CanBeParried =>
        state == State.Homing && Time.time - lastParryTime >= settings.parryLockout;

    private float CruiseSpeed =>
        Mathf.Min(settings.cruiseSpeed + settings.speedGainPerParry * parryCount, settings.maxCruiseSpeed);

    private Rigidbody rb;
    private State state = State.Homing;

    private Vector3 direction;
    private float speed;

    private float homingWeight = 1f;
    private float timeSinceParry = 999f;

    private float hitStopTimer;
    private Vector3 pendingDirection;

    private float lastParryTime = -999f;
    private int parryCount;

    private void Reset()
    {
        ConfigureRigidbody();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ConfigureRigidbody();

        direction = transform.forward;
        speed = CruiseSpeed;
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

    /// <summary>Para spawners: fija objetivo y dirección inicial.</summary>
    public void Initialize(Transform newTarget, Vector3 initialDirection)
    {
        target = newTarget;
        if (initialDirection.sqrMagnitude > 1e-4f)
        {
            direction = initialDirection.normalized;
            rb.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
        speed = CruiseSpeed;
        homingWeight = 1f;
        timeSinceParry = 999f;
        state = State.Homing;
    }

    public void SetTarget(Transform newTarget) => target = newTarget;

    /// <summary>
    /// Intenta parrear el proyectil. Devuelve false si no es posible ahora mismo.
    /// </summary>
    public bool Parry(Transform newTarget, Vector3 aimDirection)
    {
        if (!CanBeParried) return false;

        // Si no hay dirección válida, devolvemos el proyectil por donde vino.
        pendingDirection = aimDirection.sqrMagnitude > 1e-4f ? aimDirection.normalized : -direction;

        target = newTarget;
        parryCount++;
        lastParryTime = Time.time;

        state = State.HitStop;
        hitStopTimer = settings.hitStopDuration;
        rb.linearVelocity = Vector3.zero;

        Parried?.Invoke(this, parryCount);
        return true;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        if (state == State.HitStop)
        {
            rb.linearVelocity = Vector3.zero;
            hitStopTimer -= dt;
            if (hitStopTimer <= 0f) ReleaseFromHitStop();
            return;
        }

        UpdateHomingWeight(dt);
        Steer(dt);
        ApplyMotion();
    }

    private void ReleaseFromHitStop()
    {
        direction = pendingDirection;
        speed = CruiseSpeed * settings.parryBoostMultiplier;

        // El homing arranca en 0 y se recupera según la curva.
        homingWeight = 0f;
        timeSinceParry = 0f;

        state = State.Homing;
        ApplyMotion();
    }

    private void UpdateHomingWeight(float dt)
    {
        timeSinceParry += dt;
        float t = settings.homingRecoveryTime > 0f
            ? Mathf.Clamp01(timeSinceParry / settings.homingRecoveryTime)
            : 1f;
        homingWeight = settings.homingRecoveryCurve.Evaluate(t);
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

                float turn = settings.turnRate * homingWeight;
                if (distance < settings.closeRange)
                    turn *= settings.closeRangeTurnMultiplier;

                direction = Vector3.RotateTowards(
                    direction, desired, turn * Mathf.Deg2Rad * dt, 0f).normalized;

                // Frenado suave según alineación, atenuado mientras el homing se recupera.
                float curveFactor = settings.slowdownByAlignment.Evaluate(alignment);
                slowdown = Mathf.Lerp(1f, curveFactor, homingWeight);
            }
        }

        float targetSpeed = CruiseSpeed * slowdown;

        // Suavizado independiente del framerate.
        float k = 1f - Mathf.Exp(-settings.speedSmoothing * dt);
        speed = Mathf.Lerp(speed, targetSpeed, k);
    }

    private void ApplyMotion()
    {
        rb.linearVelocity = direction * speed;
        rb.MoveRotation(Quaternion.LookRotation(direction, Vector3.up));
    }

    private void OnCollisionEnter(Collision collision)
    {
        Hit?.Invoke(this, collision);

        if (bounceOnCollision && collision.contactCount > 0)
        {
            direction = Vector3.Reflect(direction, collision.GetContact(0).normal).normalized;
            ApplyMotion();
        }
    }
}