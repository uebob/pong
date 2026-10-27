using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Cuenta cuántos pongs apuntan a cada objeto.</summary>
public static class TargetMarkerRegistry
{
    private static readonly Dictionary<Transform, int> counts = new Dictionary<Transform, int>();

    /// <summary>(raíz del objetivo, nº de pongs que lo apuntan)</summary>
    public static event Action<Transform, int> CountChanged;

    public static int GetCount(Transform root) =>
        root != null && counts.TryGetValue(root, out int c) ? c : 0;

    public static void Add(Transform root)
    {
        if (root == null) return;
        counts.TryGetValue(root, out int c);
        counts[root] = ++c;
        CountChanged?.Invoke(root, c);
    }

    public static void Remove(Transform root)
    {
        if (root == null || !counts.TryGetValue(root, out int c)) return;
        c--;
        if (c <= 0) counts.Remove(root); else counts[root] = c;
        CountChanged?.Invoke(root, Mathf.Max(c, 0));
    }

    // Por si desactivas "Domain Reload" en Project Settings > Editor > Enter Play Mode.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        counts.Clear();
        CountChanged = null;
    }
}