using System;
using UnityEngine;

/// <summary>
/// El enemigo recibe el golpe de los pongs que lo tienen como objetivo y, tras la colisión,
/// los parrea devolviéndolos por donde vinieron. Así el pong aplica su daño antes de ser desviado.
/// Si el golpe mata al enemigo no hay parry (EnemyHealth guarda el pong en el inventario de su dueño).
/// Debe estar en el mismo GameObject que el collider (o que su Rigidbody) para recibir OnCollisionEnter.
/// </summary>
public class EnemyParry : MonoBehaviour
{
    [Tooltip("Tiempo mínimo entre parries (s).")]
    [SerializeField] private float parryCooldown = 0.5f;

    public event Action<PongProjectile> Parried;

    private Health health;
    private float nextParryTime;

    private PongProjectile pendingPong;
    private Vector3 pendingIncoming;

    private void Awake()
    {
        health = GetComponentInParent<Health>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (Time.time < nextParryTime) return;

        var projectile = collision.collider.GetComponentInParent<PongProjectile>();
        if (projectile == null || !projectile.CanBeParried) return;

        // Solo los proyectiles que me tienen a mí como objetivo.
        if (!IsMyTarget(projectile.Target)) return;

        // Dirección con la que llegó. Se usa la velocidad relativa de la colisión (previa al impacto)
        // y no la del Rigidbody, porque el pong puede haber rebotado ya en su propio OnCollisionEnter.
        Vector3 incoming = collision.relativeVelocity;
        if (incoming.sqrMagnitude < 0.01f) return;

        // El signo de relativeVelocity depende de qué cuerpo se tome de referencia: lo orientamos hacia mí.
        Vector3 toMe = transform.position - projectile.transform.position;
        if (Vector3.Dot(incoming, toMe) < 0f) incoming = -incoming;

        // El parry se hace en el siguiente FixedUpdate: el orden entre los OnCollisionEnter del pong y
        // del enemigo no está garantizado, y así el daño (y una posible muerte) ya están resueltos.
        pendingPong = projectile;
        pendingIncoming = incoming;
    }

    private void FixedUpdate()
    {
        if (pendingPong == null) return;

        PongProjectile projectile = pendingPong;
        Vector3 incoming = pendingIncoming;
        pendingPong = null;

        if (health != null && health.IsDead) return;   // murió con el golpe: el pong ya no se parrea
        if (!projectile.CanBeParried) return;          // guardado, destruido, ya parreado...

        // Lo devolvemos por donde vino.
        if (projectile.Parry(-incoming.normalized))
        {
            nextParryTime = Time.time + parryCooldown;
            projectile.ChangeFaction(PongFaction.Enemy);
            Parried?.Invoke(projectile);
        }
    }

    /// <summary>El target puede ser este objeto o uno de sus hijos (collider, punto de impacto...).</summary>
    private bool IsMyTarget(Transform target)
    {
        return target != null && (target == transform || target.IsChildOf(transform));
    }
}