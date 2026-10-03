using System;
using UnityEngine;

/// <summary>
/// Estadísticas numéricas del JUGADOR que los objetos pasivos pueden modificar
/// (equivalente a PongStat, pero para el movimiento y las habilidades del jugador).
///
/// Cada una se lee en PlayerMovement así: valor base (campo del inspector) -> pasivos -> valor final.
/// Un entero es el valor redondeado. Para añadir una estadística nueva:
///   1. Añade una entrada AL FINAL de este enum (no reordenes ni asignes números a mano).
///   2. En el script que la use, crea una propiedad con Stat(PlayerStat.X, baseValue)
///      y úsala en lugar del campo base.
///
/// No se exponen estadísticas donde 0 significa "sin límite" (p. ej. batMaxSpeed): un pasivo
/// con Add convertiría "sin límite" en un límite sin querer.
/// </summary>
public enum PlayerStat
{
    // --- Velocidad ---
    MoveSpeed,            // maxSpeed: velocidad de carrera

    // --- Saltos ---
    MaxJumps,             // entero: saltos totales antes de tocar suelo (Add +1 = un salto extra)
    JumpHeight,           // metros

    // --- Dash ---
    DashSpeed,
    DashDuration,         // segundos
    DashCooldown,         // segundos
    MaxAirDashes,         // entero: dashes en el aire antes de tocar suelo

    // --- Batjump ---
    BatRange,             // metros: distancia máxima a la superficie
    BatCooldown,          // segundos
    BatSpeedMultiplier,   // multiplica la velocidad que llevas al batear
    BatMinSpeed,          // velocidad mínima de salida
}

/// <summary>
/// Un cambio sobre una estadística del jugador, igual que StatModifier para los pongs.
/// Add: se suma al valor base. Multiply: multiplica el resultado (1 = sin cambio, 1.2 = +20 %).
/// Orden de resolución: (base + suma de Add) × producto de Multiply.
/// </summary>
[Serializable]
public struct PlayerStatModifier
{
    public PlayerStat stat;
    public ModifierOp op;
    [Tooltip("Add: cantidad a sumar. Multiply: factor (1 = sin cambio).")]
    public float value;
}
