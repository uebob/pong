using UnityEngine;

/// <summary>
/// Cura al recoger el objeto. Con el item marcado como "consumable" es una poción de un solo uso.
/// Crear con: Assets > Create > Pong > Effects > Heal
/// </summary>
[CreateAssetMenu(menuName = "Pong/Passive Effects/Heal", fileName = "HealEffect")]
public class HealEffect : PassiveEffect
{
    [SerializeField] private float amount = 50f;

    [Tooltip("Si es true, no se puede recoger con la vida al máximo (el objeto se queda en el suelo).")]
    [SerializeField] private bool onlyIfDamaged = true;

    public override bool CanAcquire(PlayerContext ctx)
    {
        if (!onlyIfDamaged) return true;
        return ctx.Health != null && ctx.Health.health < ctx.Health.maxHealth;
    }

    public override void OnAcquired(PlayerContext ctx)
    {
        if (ctx.Health != null) ctx.Health.Heal(amount);
    }
}
