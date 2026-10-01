using UnityEngine;

/// <summary>
/// Un comportamiento modular de pong (explotar, dividirse, atravesar...).
///
/// - Es un ScriptableObject SIN estado: un único asset se comparte entre todos los
///   proyectiles que lo usan. Los parámetros "de diseño" van en campos del asset;
///   los numéricos que los pasivos puedan modificar, mejor como PongStat.
/// - Si un behavior necesita estado POR proyectil (cooldowns, contadores...), usa
///   pong.GetState&lt;T&gt;(this), que crea y guarda una instancia de T por pong.
/// - Un pong obtiene behaviors de su PongDefinition (innatos) y de los PassiveItem
///   de su dueño (concedidos). Si varios orígenes dan el mismo asset, solo se aplica una vez.
///
/// Para crear uno nuevo: hereda de esta clase, sobrescribe los ganchos que necesites
/// y añade [CreateAssetMenu].
/// </summary>
public abstract class PongBehavior : ScriptableObject
{
    public virtual void OnLaunched(PongProjectile pong) { }
    public virtual void OnParried(PongProjectile pong, int parryCount) { }
    public virtual void OnHit(PongProjectile pong, Collision collision) { }
    public virtual void OnStored(PongProjectile pong) { }
    public virtual void Tick(PongProjectile pong, float deltaTime) { }
}
