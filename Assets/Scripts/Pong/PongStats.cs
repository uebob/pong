using System;
using System.Collections.Generic;

/// <summary>
/// Todas las estadísticas numéricas de un pong.
/// - Un bool es "valor > 0" y un int es el valor redondeado.
/// - Para añadir una estadística nueva: añade una entrada AL FINAL de este enum
///   (no reordenes ni asignes números a mano) y léela con pong.Stats.Get(PongStat.X).
///   No hay que tocar nada más: definiciones, pasivos y behaviors ya la pueden usar.
/// </summary>
public enum PongStat
{
    CruiseSpeed,
    MaxCruiseSpeed,        // 0 = sin límite
    SpeedGainPerParry,
    ParryBoostMultiplier,
    TurnRate,              // grados/s a pleno homing
    Damage,
    ExplosionRadius,       // > 0 activa la explosión de ExplodeOnHitBehavior
    SplitCount,            // reservado: división por golpe
}

/// <summary>
/// Add: se suma al valor base. Multiply: multiplica el resultado (1 = sin cambio, 1.2 = +20 %).
/// Orden de resolución: (base + suma de Add) * producto de Multiply.
/// </summary>
public enum ModifierOp { Add, Multiply }

[Serializable]
public struct StatValue
{
    public PongStat stat;
    public float value;
}

[Serializable]
public struct StatModifier
{
    public PongStat stat;
    public ModifierOp op;
    [UnityEngine.Tooltip("Add: cantidad a sumar. Multiply: factor (1 = sin cambio).")]
    public float value;
}

/// <summary>Valores ya resueltos (base + pasivos) de un pong concreto.</summary>
public sealed class PongStatSheet
{
    private static readonly int Count = ComputeCount();
    private readonly float[] values = new float[Count];

    public float Get(PongStat stat) => values[(int)stat];
    public int GetInt(PongStat stat) => UnityEngine.Mathf.RoundToInt(values[(int)stat]);
    public bool GetBool(PongStat stat) => values[(int)stat] > 0f;

    public static PongStatSheet Build(PongDefinition definition, IReadOnlyList<PassiveItem> passives)
    {
        var sheet = new PongStatSheet();
        var add = new float[Count];
        var mul = new float[Count];
        for (int i = 0; i < Count; i++) mul[i] = 1f;

        if (definition != null && definition.baseStats != null)
            foreach (var sv in definition.baseStats)
                sheet.values[(int)sv.stat] = sv.value;

        if (passives != null)
        {
            foreach (var passive in passives)
            {
                if (passive == null || passive.statModifiers == null) continue;

                foreach (var m in passive.statModifiers)
                {
                    int i = (int)m.stat;
                    if (m.op == ModifierOp.Add) add[i] += m.value;
                    else mul[i] *= m.value;
                }
            }
        }

        for (int i = 0; i < Count; i++)
            sheet.values[i] = (sheet.values[i] + add[i]) * mul[i];

        return sheet;
    }

    private static int ComputeCount()
    {
        int max = 0;
        foreach (int v in Enum.GetValues(typeof(PongStat)))
            if (v > max) max = v;
        return max + 1;
    }
}
