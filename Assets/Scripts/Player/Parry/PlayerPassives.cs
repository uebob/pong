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

    public void Add(PassiveItem item)
    {
        if (item == null) return;
        items.Add(item);
        Changed?.Invoke();
    }

    public bool Remove(PassiveItem item)
    {
        bool removed = items.Remove(item);
        if (removed) Changed?.Invoke();
        return removed;
    }
}
