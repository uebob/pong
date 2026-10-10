using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implementa esto en tus enemigos si quieres controlar tú cómo reaccionan al golpe
/// (p. ej. enemigos con NavMeshAgent o CharacterController, que no responden a fuerzas físicas).
/// </summary>
public interface IKnockbackable
{
    /// <param name="velocity">Velocidad de empuje (m/s), ya con componente vertical.</param>
    /// <param name="hitPoint">Punto aproximado del impacto.</param>
    void ApplyKnockback(Vector3 velocity, Vector3 hitPoint);
}

/// <summary>
/// Golpe de bate: clic izquierdo (el mismo botón que el batjump) aplica knockback a los enemigos
/// delante del jugador. Es independiente del parry (ambos pueden ocurrir en el mismo clic).
/// Si el golpe alcanza a algún enemigo, ese clic NO hace batjump.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PlayerBatHit : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Se busca solo si está vacío (en este objeto o en sus padres).")]
    [SerializeField] private PlayerMovement movement;
    [Tooltip("Si está vacío usa movement.cameraTransform o Camera.main.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Detección")]
    [Tooltip("Layer(s) de los enemigos. Mejor layer que tag: el filtro se hace dentro de la física.")]
    [SerializeField] private LayerMask enemyMask;
    [Tooltip("Opcional: si no está vacío, además el objeto debe tener este tag.")]
    [SerializeField] private string requiredTag = "";
    [SerializeField] private float hitRange = 2.5f;
    [Tooltip("1 = solo justo delante, 0 = hemisferio frontal, -1 = cualquier dirección.")]
    [Range(-1f, 1f)]
    [SerializeField] private float minFacingDot = 0.3f;
    [SerializeField] private int maxTargets = 3;
    [Tooltip("Si no está vacío, un enemigo tapado por estas capas (paredes) no se golpea.")]
    [SerializeField] private LayerMask obstructionMask;

    [Header("Knockback")]
    [SerializeField] private float knockbackSpeed = 14f;
    [Tooltip("Componente vertical añadida a la dirección del empuje (0 = horizontal puro).")]
    [SerializeField] private float knockbackUpward = 0.35f;
    [Tooltip("Si un enemigo no implementa IKnockbackable, se usa su Rigidbody (si no es kinematic).")]
    [SerializeField] private bool fallbackToRigidbody = true;

    [Header("Golpe")]
    [SerializeField] private float hitCooldown = 0.3f;
    [Tooltip("Si es true, el clic que golpea a un enemigo NO hace batjump (igual que el parry).")]
    [SerializeField] private bool consumeBatInputOnHit = true;
    public AudioClip hitSoundEffect;

    /// <summary>(enemigo golpeado, punto de impacto). Útil para daño, FX, animación...</summary>
    public event Action<GameObject, Vector3> EnemyHit;

    private Collider col;
    private AudioSource audioSource;
    private float lastHitTime = -10f;

    private readonly Collider[] overlapBuffer = new Collider[16];
    private readonly List<(Transform root, Collider c, float dist)> candidates = new List<(Transform, Collider, float)>(16);
    private readonly HashSet<Transform> alreadyHit = new HashSet<Transform>();

    private void Awake()
    {
        col = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();
        if (movement == null) movement = GetComponentInParent<PlayerMovement>();
        if (cameraTransform == null && movement != null) cameraTransform = movement.cameraTransform;
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        if (movement == null || cameraTransform == null) return;
        if (!Input.GetKeyDown(movement.batkey)) return;

        // Tiempo real: durante un freeze (timeScale 0) el cooldown escalado no avanzaría.
        if (Time.unscaledTime - lastHitTime < hitCooldown) return;

        if (TryHit())
        {
            lastHitTime = Time.unscaledTime;

            // El clic ya se ha usado para golpear: que no haga también batjump.
            // Funciona sea cual sea el orden de Update respecto a PlayerMovement.
            if (consumeBatInputOnHit)
                movement.ConsumeBatInput();
        }
    }

    private bool TryHit()
    {
        Vector3 center = col.bounds.center;
        Vector3 facing = cameraTransform.forward;

        int count = Physics.OverlapSphereNonAlloc(center, hitRange, overlapBuffer, enemyMask, QueryTriggerInteraction.Ignore);

        candidates.Clear();
        for (int i = 0; i < count; i++)
        {
            Collider c = overlapBuffer[i];
            if (c.transform.IsChildOf(transform)) continue; // por si el jugador está en la máscara

            if (requiredTag.Length > 0 && !c.CompareTag(requiredTag) && !c.transform.root.CompareTag(requiredTag))
                continue;

            Vector3 point = c.ClosestPoint(center);
            Vector3 toEnemy = point - center;
            float dist = toEnemy.magnitude;
            if (dist > 0.001f && Vector3.Dot(facing, toEnemy / dist) < minFacingDot) continue;

            if (obstructionMask.value != 0 && dist > 0.001f &&
                Physics.Raycast(center, toEnemy / dist, dist, obstructionMask, QueryTriggerInteraction.Ignore))
                continue;

            Transform root = ResolveRoot(c);
            candidates.Add((root, c, dist));
        }

        if (candidates.Count == 0) return false;

        candidates.Sort((a, b) => a.dist.CompareTo(b.dist));
        alreadyHit.Clear();

        int hits = 0;
        foreach (var cand in candidates)
        {
            if (hits >= maxTargets) break;
            if (!alreadyHit.Add(cand.root)) continue; // un enemigo con varios colliders solo se golpea una vez

            Vector3 hitPoint = cand.c.ClosestPoint(center);
            Vector3 dir = cand.root.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.ProjectOnPlane(facing, Vector3.up);
            dir = (dir.normalized + Vector3.up * knockbackUpward).normalized;

            Knock(cand.root, dir * knockbackSpeed, hitPoint);
            EnemyHit?.Invoke(cand.root.gameObject, hitPoint);
            hits++;
        }

        if (hits > 0 && audioSource != null && hitSoundEffect != null)
            audioSource.PlayOneShot(hitSoundEffect);

        return hits > 0;
    }

    private static Transform ResolveRoot(Collider c)
    {
        var k = c.GetComponentInParent<IKnockbackable>();
        if (k is Component comp) return comp.transform;
        return c.attachedRigidbody != null ? c.attachedRigidbody.transform : c.transform;
    }

    private void Knock(Transform target, Vector3 velocity, Vector3 hitPoint)
    {
        var k = target.GetComponentInParent<IKnockbackable>();
        if (k != null)
        {
            k.ApplyKnockback(velocity, hitPoint);
            return;
        }

        if (!fallbackToRigidbody) return;
        var erb = target.GetComponentInParent<Rigidbody>();
        if (erb != null && !erb.isKinematic)
            erb.AddForce(velocity, ForceMode.VelocityChange);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hitRange);
    }
}