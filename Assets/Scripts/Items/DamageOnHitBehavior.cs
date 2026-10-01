using UnityEngine;

/// <summary>
/// Daño directo al golpear algo con IPongDamageable. Ejemplo de behavior con
/// estado POR proyectil (el cooldown) usando pong.GetState&lt;T&gt;(this), para
/// que un pong que rebota no golpee varias veces seguidas al mismo objetivo.
/// </summary>
[CreateAssetMenu(menuName = "Pong/Behaviors/Damage On Hit", fileName = "DamageOnHit")]
public class DamageOnHitBehavior : PongBehavior
{
    [SerializeField] private float hitCooldown = 0.25f;

    private class State
    {
        public float nextHitTime;
    }

    public override void OnHit(PongProjectile pong, Collision collision)
    {
        var target = collision.collider.GetComponentInParent<IPongDamageable>();
        if (target == null) return;

        // Quién recibe daño lo decide el receptor (ver IPongDamageable).
        var state = pong.GetState<State>(this);
        if (Time.time < state.nextHitTime) return;
        state.nextHitTime = Time.time + hitCooldown;

        target.TakeDamage(pong.Stats.Get(PongStat.Damage), pong);
    }
}