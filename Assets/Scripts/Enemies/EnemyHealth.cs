using UnityEngine;

/// <summary>
/// Vida de un enemigo. Recibe daño de cualquier pong. Si un pong lo mata, ese pong se guarda
/// en el inventario de su dueño (si lo tiene y hay hueco) en lugar de seguir rebotando.
/// </summary>
public class EnemyHealth : Health
{
    public override void TakeDamage(float amount, PongProjectile source)
    {
        bool wasDead = IsDead;

        base.TakeDamage(amount, source);

        // Solo en el golpe que mata
        if (wasDead || !IsDead || source == null) return;

        if (source.Owner != null && source.Owner.TryGetComponent(out PongInventory inventory))
            inventory.TryStore(source);
    }
}