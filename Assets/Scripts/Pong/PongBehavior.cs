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
    /// <summary>
    /// Daño del pong ahora mismo: (Damage + velocidad actual × SpeedDamageScale) × multiplier.
    /// - Damage y SpeedDamageScale son estadísticas del pong (los pasivos pueden modificarlas).
    /// - multiplier es un ajuste propio de cada behavior (p. ej. explosión = 0.5, golpe directo = 1).
    /// Úsalo en todos los behaviors que hagan daño: así el parry (que sube la velocidad) y los
    /// pasivos afectan a todos por igual.
    /// </summary>
    protected static float CurrentDamage(PongProjectile pong, float multiplier = 1f) =>
        (pong.Stats.Get(PongStat.Damage) +
         pong.CurrentSpeed * pong.Stats.Get(PongStat.SpeedDamageScale)) * multiplier;

    public virtual void OnLaunched(PongProjectile pong) { }
    public virtual void OnParried(PongProjectile pong, int parryCount) { }
    public virtual void OnHit(PongProjectile pong, Collision collision) { }
    public virtual void OnStored(PongProjectile pong) { }
    public virtual void Tick(PongProjectile pong, float deltaTime) { }
}