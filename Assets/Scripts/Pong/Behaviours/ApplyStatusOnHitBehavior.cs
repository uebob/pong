using UnityEngine;

/// <summary>
/// Aplica un efecto de estado (StatusEffectSpec: tipo, magnitud, duración) al chocar.
/// Es solo un envoltorio: toda la lógica de los efectos vive en StatusEffects.
///
/// - HitTarget: lo que el pong golpea. Si tiene Health, el efecto pasa por Health.ReceiveStatus
///   (así el jugador espera su margen de parry, igual que con el daño). Si solo tiene StatusEffects, se aplica directo.
/// - Owner: quien lanzó el pong (p. ej. un pong que da Haste a su dueño al golpear algo).
///
/// Como los efectos se apilan sin límite, hay un cooldown por pong: si no, un rebote contra
/// el mismo objetivo lo apilaría varias veces seguidas.
/// </summary>
[CreateAssetMenu(menuName = "Pong/Behaviors/Apply Status On Hit", fileName = "ApplyStatusOnHit")]
public class ApplyStatusOnHitBehavior : PongBehavior
{
    public enum Recipient { HitTarget, Owner }

    [SerializeField] private StatusEffectSpec effect =
        new StatusEffectSpec(StatusEffectType.Slowness, 0.3f, 3f);

    [SerializeField] private Recipient recipient = Recipient.HitTarget;

    [Tooltip("Tiempo mínimo entre dos aplicaciones del mismo pong (s), para que un rebote no apile el efecto varias veces.")]
    [SerializeField] private float hitCooldown = 0.25f;

    private class State
    {
        public float nextApplyTime;
    }

    public override void OnHit(PongProjectile pong, Collision collision)
    {
        var state = pong.GetState<State>(this);
        if (Time.time < state.nextApplyTime) return;

        if (recipient == Recipient.Owner)
        {
            if (pong.Owner == null) return;

            StatusEffects ownerEffects = pong.Owner.GetComponentInParent<StatusEffects>();
            if (ownerEffects == null) return;

            state.nextApplyTime = Time.time + hitCooldown;
            ownerEffects.Apply(effect);
            return;
        }

        // Lo golpeado: preferimos pasar por Health para respetar el margen de parry del jugador.
        Health health = collision.collider.GetComponentInParent<Health>();
        if (health != null)
        {
            state.nextApplyTime = Time.time + hitCooldown;
            health.ReceiveStatus(effect, pong);
            return;
        }

        StatusEffects targetEffects = collision.collider.GetComponentInParent<StatusEffects>();
        if (targetEffects == null) return;

        state.nextApplyTime = Time.time + hitCooldown;
        targetEffects.Apply(effect);
    }
}
