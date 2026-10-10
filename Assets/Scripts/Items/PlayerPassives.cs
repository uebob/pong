using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pasivos activos del jugador. Los enemigos pueden implementar IPongModifierProvider
/// igual para tener sus propios pasivos.
/// Los pongs resuelven sus estadísticas al lanzarse (y al cambiar de dueño), así que un
/// pasivo recogido afecta a todos los pongs guardados en el siguiente lanzamiento.
/// </summary>
public class PlayerPassives : MonoBehaviour, IPongModifierProvider
{
    [SerializeField] private List<PassiveItem> items = new List<PassiveItem>();

    public IReadOnlyList<PassiveItem> ActivePassives => items;
    public event Action Changed;

    /// <summary>
    /// Aplica los pasivos activos a un valor base del jugador: (base + suma de Add) × producto de Multiply.
    /// Se calcula en cada llamada (las listas son pequeñas), así que cualquier cambio en la lista,
    /// incluso editándola en el inspector en juego, se refleja al instante.
    /// </summary>
    public float Apply(PlayerStat stat, float baseValue)
    {
        float add = 0f;
        float mul = 1f;

        for (int i = 0; i < items.Count; i++)
        {
            PassiveItem item = items[i];
            if (item == null || item.playerModifiers == null) continue;

            List<PlayerStatModifier> modifiers = item.playerModifiers;
            for (int j = 0; j < modifiers.Count; j++)
            {
                if (modifiers[j].stat != stat) continue;

                if (modifiers[j].op == ModifierOp.Add) add += modifiers[j].value;
                else mul *= modifiers[j].value;
            }
        }

        return (baseValue + add) * mul;
    }

    /// <summary>Igual que Apply, para estadísticas enteras (resultado redondeado).</summary>
    public int ApplyInt(PlayerStat stat, int baseValue) =>
        Mathf.RoundToInt(Apply(stat, baseValue));

    /// <summary>
    /// Añade el item. Devuelve false si no se pudo (item nulo, o no apilable y ya lo tienes).
    /// </summary>
    public bool Add(PassiveItem item)
    {
        if (item == null) return false;
        if (!item.stackable && items.Contains(item)) return false;

        items.Add(item);
        Changed?.Invoke();
        return true;
    }

    public bool Remove(PassiveItem item)
    {
        bool removed = items.Remove(item);
        if (removed) Changed?.Invoke();
        return removed;
    }
}