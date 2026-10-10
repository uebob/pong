using UnityEngine;

/// <summary>
/// Efecto con lógica de un objeto pasivo (curar, reaccionar a un parry, etc.).
/// Es el equivalente de PongBehavior, pero para el JUGADOR en lugar de para un pong.
///
/// - ScriptableObject SIN estado: un único asset se comparte entre todos los usos.
///   Si necesitas estado (contadores, cooldowns), usa ctx.GetState&lt;T&gt;(this).
/// - Cuándo usar cada mecanismo:
///     * Un número que cambia (velocidad, saltos...)  -> PlayerStat (playerModifiers del item).
///     * Un interruptor que alguien consulta (mapa visible) -> PlayerStat usado como bandera (> 0).
///     * Algo que ocurre al recoger o al hacer una acción -> un PassiveEffect.
/// - Si un item es stackable y lo tienes dos veces, sus efectos se ejecutan dos veces.
///
/// Para crear uno nuevo: hereda de esta clase, sobrescribe los ganchos que necesites
/// y añade [CreateAssetMenu]. Para un gancho nuevo: añádelo aquí, y que PlayerPassives lo despache
/// desde el evento correspondiente del jugador.
/// </summary>
public abstract class PassiveEffect : ScriptableObject
{
    /// <summary>
    /// Si devuelve false, el objeto no se puede recoger (se queda en el suelo).
    /// Ej.: una cura no se recoge con la vida al máximo.
    /// </summary>
    public virtual bool CanAcquire(PlayerContext ctx) => true;

    /// <summary>Al recoger el objeto (y al empezar la partida, si ya estaba en la lista inicial).</summary>
    public virtual void OnAcquired(PlayerContext ctx) { }

    /// <summary>Al quitar el objeto (PlayerPassives.Remove).</summary>
    public virtual void OnRemoved(PlayerContext ctx) { }

    public virtual void OnParry(PlayerContext ctx, PongProjectile pong) { }
    public virtual void OnDamaged(PlayerContext ctx, float amount) { }
    public virtual void OnBatJump(PlayerContext ctx, Vector3 point, Vector3 normal) { }
    public virtual void OnJump(PlayerContext ctx) { }
    public virtual void OnDash(PlayerContext ctx) { }
    public virtual void OnGroundPoundLand(PlayerContext ctx) { }
}
