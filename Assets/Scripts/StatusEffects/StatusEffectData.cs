using System;
using UnityEngine;

/// <summary>
/// Efectos de estado disponibles. Para añadir uno nuevo: añade una entrada AL FINAL del enum,
/// decide si es dañino (StatusEffectInfo.IsHarmful) y dale su lógica en StatusEffects.
/// </summary>
public enum StatusEffectType
{
    Slowness,       // magnitud: fracción de velocidad que se pierde (0.3 = -30 %)
    Haste,          // magnitud: fracción de velocidad que se gana (0.5 = +50 %)
    Weakness,       // magnitud: fracción EXTRA de daño recibido (0.5 = +50 % de daño recibido)
    Resistance,     // magnitud: fracción de daño recibido que se evita (0.3 = -30 % de daño recibido)
    Freeze,         // bloqueo total. La magnitud se ignora
    Burn,           // magnitud: daño por segundo (se aplica en ticks)
    Regeneration,   // magnitud: vida por segundo (se aplica en ticks)
    Invincibility,  // no recibe daño ni efectos dañinos. La magnitud se ignora
}

/// <summary>
/// Un efecto listo para aplicar: tipo, magnitud y duración. Es serializable, así que se puede
/// poner en el inspector (behaviors, items, trampas...) y pasar tal cual a StatusEffects.Apply.
/// </summary>
[Serializable]
public struct StatusEffectSpec
{
    public StatusEffectType type;

    [Tooltip("Slowness: fracción que se pierde (0.3 = -30 %). Haste: fracción que se gana. " +
             "Weakness: fracción extra de daño recibido. Resistance: fracción de daño que se evita. " +
             "Burn: daño por segundo. Regeneration: vida por segundo. Freeze e Invincibility: se ignora.")]
    public float magnitude;

    [Tooltip("Duración en segundos.")]
    public float duration;

    public StatusEffectSpec(StatusEffectType type, float magnitude, float duration)
    {
        this.type = type;
        this.magnitude = magnitude;
        this.duration = duration;
    }
}

public static class StatusEffectInfo
{
    /// <summary>Los efectos dañinos quedan latentes mientras hay Invincibility (y vuelven al terminar).</summary>
    public static bool IsHarmful(StatusEffectType type) =>
        type == StatusEffectType.Slowness ||
        type == StatusEffectType.Weakness ||
        type == StatusEffectType.Freeze ||
        type == StatusEffectType.Burn;

    /// <summary>Los que actúan por ticks (no por multiplicadores continuos).</summary>
    public static bool UsesTicks(StatusEffectType type) =>
        type == StatusEffectType.Burn ||
        type == StatusEffectType.Regeneration;
}
