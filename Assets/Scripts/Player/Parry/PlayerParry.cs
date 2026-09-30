using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerParry : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Añade más bindings (mando, ratón...) desde el inspector.")]
    [SerializeField] private InputAction parryAction =
        new InputAction("Parry", InputActionType.Button, "<Keyboard>/f");

    [Header("Parry")]
    [SerializeField] private float parryRadius = 3f;
    [SerializeField] private LayerMask projectileLayer;
    [Tooltip("Tiempo que la ventana de parry permanece activa tras pulsar (s).")]
    [SerializeField] private float parryWindow = 0.2f;
    [Tooltip("Tiempo mínimo entre pulsaciones (s).")]
    [SerializeField] private float parryCooldown = 0.4f;
    [Tooltip("Proyectiles máximos por pulsación.")]
    [SerializeField] private int maxProjectilesPerParry = 1;
    [Tooltip("1 = solo justo delante, 0 = hemisferio frontal, -1 = cualquier dirección.")]
    [Range(-1f, 1f)]
    [SerializeField] private float minFacingDot = 0f;

    [Header("Aiming")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform currentEnemyTarget;
    [Tooltip("Si el enemigo está dentro de este ángulo respecto a la cámara, se aplica ayuda de puntería.")]
    [SerializeField] private float aimAssistAngle = 25f;
    [Range(0f, 1f)]
    [SerializeField] private float aimAssistStrength = 0.6f;

    /// <summary>Al pulsar el parry (abre la ventana). Útil para animación.</summary>
    public event Action ParryStarted;

    /// <summary>Al parrear con éxito un proyectil. Útil para VFX, audio, screenshake.</summary>
    public event Action<PongProjectile> ParrySucceeded;

    private readonly Collider[] hitBuffer = new Collider[16];
    private float windowTimer;
    private float cooldownTimer;
    private int parriedThisWindow;

    private void OnEnable() => parryAction.Enable();
    private void OnDisable() => parryAction.Disable();

    public void SetEnemyTarget(Transform enemy) => currentEnemyTarget = enemy;

    private void Update()
    {
        float dt = Time.deltaTime;
        cooldownTimer -= dt;

        if (parryAction.WasPressedThisFrame() && cooldownTimer <= 0f)
        {
            windowTimer = parryWindow;
            cooldownTimer = parryCooldown;
            parriedThisWindow = 0;
            ParryStarted?.Invoke();
        }

        if (windowTimer > 0f)
        {
            windowTimer -= dt;
            TryParry();
        }
    }

    private void TryParry()
    {
        Vector3 facing = playerCamera != null ? playerCamera.forward : transform.forward;

        int count = Physics.OverlapSphereNonAlloc(
            transform.position, parryRadius, hitBuffer, projectileLayer, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            var projectile = hitBuffer[i].GetComponentInParent<PongProjectile>();
            if (projectile == null || !projectile.CanBeParried) continue;

            // Debe estar delante del jugador.
            Vector3 toProjectile = (projectile.transform.position - transform.position).normalized;
            if (Vector3.Dot(facing, toProjectile) < minFacingDot) continue;

            Vector3 aim = ComputeAimDirection(projectile.transform.position, facing);

            if (projectile.Parry(currentEnemyTarget, aim))
            {
                ParrySucceeded?.Invoke(projectile);
                parriedThisWindow++;

                if (parriedThisWindow >= maxProjectilesPerParry)
                {
                    windowTimer = 0f;
                    break;
                }
            }
        }
    }

    private Vector3 ComputeAimDirection(Vector3 fromPosition, Vector3 facing)
    {
        Vector3 aim = facing;

        if (currentEnemyTarget != null)
        {
            Vector3 toEnemy = (currentEnemyTarget.position - fromPosition).normalized;
            if (Vector3.Angle(facing, toEnemy) <= aimAssistAngle)
                aim = Vector3.Slerp(facing, toEnemy, aimAssistStrength);
        }

        return aim.normalized;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, parryRadius);
    }
}