using UnityEngine;

/// <summary>
/// El enemigo lanza un pong al jugador cada cierto intervalo, siempre que
/// el jugador esté en rango y no haya obstáculos (según obstacleMask) en medio.
/// </summary>
public class EnemyShootProjectile : MonoBehaviour
{
    [Header("Proyectil")]
    [SerializeField] private PongDefinition projectile;
    [Tooltip("Opcional; si es null se usa este transform.")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private PongFaction faction = PongFaction.Enemy;

    [Header("Disparo")]
    [SerializeField] private float fireInterval = 3f;
    [Tooltip("Retraso aleatorio extra del primer disparo, para que varios enemigos no disparen sincronizados.")]
    [SerializeField] private float initialDelayJitter = 1f;

    [Header("Detección")]
    [SerializeField] private float minDistance = 0f;
    [SerializeField] private float maxDistance = 15f;
    [Tooltip("Capas que bloquean la línea de visión (Ground, Wall...).")]
    [SerializeField] private LayerMask obstacleMask;

    private float nextFireTime;
    private Collider[] ownColliders;

    private void Awake()
    {
        ownColliders = GetComponentsInChildren<Collider>();
    }

    private void OnEnable()
    {
        nextFireTime = Time.time + Random.Range(0f, initialDelayJitter);
    }

    private void Update()
    {
        if (projectile == null || Time.time < nextFireTime) return;

        Transform player = PongTargeting.FindPlayer();
        if (player == null) return;

        Vector3 origin = FirePosition;
        Vector3 playerPos = player.position;

        if (!CanSee(origin, playerPos)) return;

        Fire(origin, playerPos);
        nextFireTime = Time.time + fireInterval;
    }

    private Vector3 FirePosition => firePoint != null ? firePoint.position : transform.position;

    private bool CanSee(Vector3 origin, Vector3 targetPos)
    {
        Vector3 toTarget = targetPos - origin;
        float distance = toTarget.magnitude;

        if (distance > maxDistance || distance < minDistance) return false;
        if (distance < 0.001f) return true;

        // Si algo de las capas de obstáculo se interpone, no hay línea de visión.
        return !Physics.Raycast(origin, toTarget / distance, distance,
                                obstacleMask, QueryTriggerInteraction.Ignore);
    }

    private void Fire(Vector3 origin, Vector3 targetPos)
    {
        Vector3 dir = (targetPos - origin).normalized;
        Quaternion rot = Quaternion.LookRotation(dir);

        PongProjectile pong = projectile.Spawn(gameObject, faction, origin, rot);
        if (pong == null) return;

        // Que el proyectil no choque con el propio enemigo.
        if (pong.TryGetComponent(out Collider projCol))
            foreach (var c in ownColliders)
                if (c != null) Physics.IgnoreCollision(projCol, c);

        // Sin objetivo explícito: vuela libre launchFreeFlightTime y luego elige el suyo.
        pong.Launch(origin, dir);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxDistance);
        if (minDistance > 0f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, minDistance);
        }
    }
}