using UnityEngine;

/// <summary>
/// Cura al parrear (cada N parries). Ejemplo de efecto REACTIVO y con estado por partida (el contador).
/// Crear con: Assets > Create > Pong > Effects > Heal On Parry
/// </summary>
[CreateAssetMenu(menuName = "Pong/Passive Effects/Heal On Parry", fileName = "HealOnParryEffect")]
public class HealOnParryEffect : PassiveEffect
{
    [SerializeField] private float amount = 5f;
    [Tooltip("Cura cada N parries (1 = en todos).")]
    [SerializeField, Min(1)] private int parriesNeeded = 1;

    private class State
    {
        public int count;
    }

    public override void OnParry(PlayerContext ctx, PongProjectile pong)
    {
        State state = ctx.GetState<State>(this);
        state.count++;
        if (state.count % parriesNeeded != 0) return;

        if (ctx.Health != null) ctx.Health.Heal(amount);
    }
}
