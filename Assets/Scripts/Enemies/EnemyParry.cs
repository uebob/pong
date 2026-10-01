using System;
using UnityEngine;

/// <summary>
/// El enemigo parrea automáticamente cualquier PongProjectile que entre en su radio,
/// devolviéndolo por donde vino (dirección opuesta a su velocidad).
/// La detección filtra por layer (LayerMask); lo que cuenta es que el objeto tenga PongProjectile.
/// </summary>
public class EnemyParry : MonoBehaviour
{
    [Header("Detección")]
    [Tooltip("Layer(s) de los proyectiles (por ejemplo 'Projectile').")]
    [SerializeField] private LayerMask projectileMask;
    [SerializeField] private float detectionRadius = 4f;
    [Tooltip("Punto desde el que se mide el radio. Si está vacío, el propio transform.")]
    [SerializeField] private Transform center;

    [Header("Parry")]
    [Tooltip("Tiempo mínimo entre parries (s).")]
    [SerializeField] private float parryCooldown = 0.5f;
    [Tooltip("Solo parrea pongs que se acercan a él (ignora los que se alejan o pasan de largo).")]
    [SerializeField] private bool onlyApproaching = true;
    [Tooltip("Evita parrear sus propios disparos recién lanzados (los suyos solo cuentan si alguien ya los parreó).")]
    [SerializeField] private bool ignoreOwnFreshProjectiles = true;

    public event Action<PongProjectile> Parried;

    private readonly Collider[] hits = new Collider[16];
    private float nextParryTime;

    private Vector3 Center => center != null ? center.position : transform.position;

    // Los pongs se mueven en FixedUpdate, así que consultamos en el mismo ciclo.
    private void FixedUpdate()
    {
        if (Time.time < nextParryTime) return;

        int count = Physics.OverlapSphereNonAlloc(
            Center, detectionRadius, hits, projectileMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            var projectile = hits[i].GetComponentInParent<PongProjectile>();
            if (projectile == null || !projectile.CanBeParried) continue;

            if (ignoreOwnFreshProjectiles && projectile.Owner == gameObject && projectile.ParryCount == 0)
                continue;

            if (!projectile.TryGetComponent(out Rigidbody projectileBody)) continue;

            Vector3 incoming = projectileBody.linearVelocity;
            if (incoming.sqrMagnitude < 0.01f) continue;

            // ¿Se acerca? (su velocidad apunta hacia nosotros)
            if (onlyApproaching && Vector3.Dot(incoming, Center - projectile.transform.position) <= 0f)
                continue;

            // Lo devolvemos por donde vino.
            if (projectile.Parry(-incoming.normalized))
            {
                nextParryTime = Time.time + parryCooldown;
                Parried?.Invoke(projectile);
                projectile.ChangeFaction(PongFaction.Enemy);
                break; // un parry por activación
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.8f);
        Vector3 c = center != null ? center.position : transform.position;
        Gizmos.DrawWireSphere(c, detectionRadius);
    }
}
