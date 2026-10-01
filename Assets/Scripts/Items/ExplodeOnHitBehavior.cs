using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Explota al colisionar. Radio y daño salen de las estadísticas del pong
/// (ExplosionRadius y Damage), así que los pasivos pueden potenciarlo.
/// Si ExplosionRadius es 0, no hace nada.
/// </summary>
[CreateAssetMenu(menuName = "Pong/Behaviors/Explode On Hit", fileName = "ExplodeOnHit")]
public class ExplodeOnHitBehavior : PongBehavior
{
    [SerializeField] private LayerMask damageMask = ~0;
    [SerializeField] private GameObject vfxPrefab;
    [SerializeField] private bool despawnAfterExplosion = true;

    public override void OnHit(PongProjectile pong, Collision collision)
    {
        float radius = pong.Stats.Get(PongStat.ExplosionRadius);
        if (radius <= 0f) return;

        float damage = pong.Stats.Get(PongStat.Damage);

        Collider[] hits = Physics.OverlapSphere(
            pong.transform.position, radius, damageMask, QueryTriggerInteraction.Ignore);

        var alreadyHit = new HashSet<IPongDamageable>();
        foreach (var col in hits)
        {
            var target = col.GetComponentInParent<IPongDamageable>();
            if (target == null || !alreadyHit.Add(target)) continue;

            // Quién recibe daño lo decide el receptor (ver IPongDamageable).
            target.TakeDamage(damage, pong);
        }

        if (vfxPrefab != null)
        {
            var vfx = Instantiate(vfxPrefab, pong.transform.position, Quaternion.identity);
            vfx.transform.localScale = Vector3.one * radius * 2f;
        }

        if (despawnAfterExplosion)
            pong.Despawn();
    }
}