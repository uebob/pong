using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class FloorGenerationSettings
{
    [Header("Tamaño del piso")]
    public int minRooms = 8;
    public int maxRooms = 12;

    [Header("Estructura")]
    [Tooltip("Probabilidad de permitir que una sala nueva toque a más de una sala existente (crea bucles).")]
    [Range(0f, 1f)] public float loopChance = 0.1f;

    [Header("Salas especiales")]
    [Tooltip("Distancia mínima (en salas) desde el inicio hasta el boss.")]
    public int minBossDistance = 4;
    [Tooltip("Distancia mínima (en salas) desde el inicio hasta la tienda.")]
    public int minShopDistance = 3;

    [Header("Salas restantes")]
    [Range(0f, 1f)] public float combatChance = 0.7f;
    [Tooltip("Si es true, las salas pegadas a la sala inicial nunca son de combate.")]
    public bool safeRoomsNextToStart = true;

    [Header("Reintentos")]
    public int maxAttempts = 100;
}

/// <summary>
/// Genera un FloorLayout (solo datos). No instancia nada en la escena.
/// Mismo seed + mismos settings = mismo piso.
/// </summary>
public class FloorGenerator
{
    private readonly FloorGenerationSettings settings;
    private System.Random rng;

    public FloorGenerator(FloorGenerationSettings settings)
    {
        this.settings = settings;
    }

    /// <summary>Devuelve el layout, o null si no se pudo generar tras todos los intentos.</summary>
    public FloorLayout Generate(int seed)
    {
        rng = new System.Random(seed);

        for (int attempt = 0; attempt < settings.maxAttempts; attempt++)
        {
            var layout = TryBuild();
            if (layout != null)
            {
                layout.Seed = seed;
                return layout;
            }
        }

        Debug.LogError($"[FloorGenerator] No se pudo generar un piso válido tras {settings.maxAttempts} intentos (seed {seed}). " +
                       "Revisa los ajustes (distancias mínimas demasiado altas para el nº de salas, etc.).");
        return null;
    }

    // ------------------------------------------------------------------
    // Un intento de generación. Devuelve null si el resultado no es válido.
    // ------------------------------------------------------------------
    private FloorLayout TryBuild()
    {
        int target = rng.Next(settings.minRooms, settings.maxRooms + 1);

        var layout = new FloorLayout();
        layout.StartRoom = layout.AddRoom(Vector2Int.zero, RoomType.Start);

        // La sala inicial tiene exactamente UN vecino: lo colocamos a mano.
        var firstDir = DirectionExtensions.All[rng.Next(4)];
        var first = layout.AddRoom(firstDir.ToVector());

        // Salas desde las que se puede seguir expandiendo (nunca la inicial).
        var expandable = new List<RoomData> { first };

        int iterations = 0;
        int maxIterations = target * 50;

        while (layout.Rooms.Count < target && iterations++ < maxIterations)
        {
            var from = expandable[rng.Next(expandable.Count)];
            var dir = DirectionExtensions.All[rng.Next(4)];
            var pos = from.Position + dir.ToVector();

            if (layout.HasRoom(pos)) continue;

            // Nada puede quedar pegado a la sala inicial (solo tiene 1 salida).
            if (IsAdjacent(pos, layout.StartRoom.Position)) continue;

            // Evitamos bloques compactos: normalmente solo 1 vecino al colocar.
            if (layout.CountNeighbors(pos) > 1 && rng.NextDouble() >= settings.loopChance) continue;

            expandable.Add(layout.AddRoom(pos));
        }

        if (layout.Rooms.Count < target) return null;

        layout.RefreshDoors();
        layout.ComputeDistances();

        if (!AssignSpecialRooms(layout)) return null;
        AssignRemainingRooms(layout);

        return layout;
    }

    // ------------------------------------------------------------------
    // Boss y tienda: siempre en callejones sin salida (1 sola puerta).
    // ------------------------------------------------------------------
    private bool AssignSpecialRooms(FloorLayout layout)
    {
        var deadEnds = layout.Rooms.Values
            .Where(r => r.Type == RoomType.None && r.IsDeadEnd)
            .ToList();

        // Barajamos primero para que los empates se resuelvan al azar
        // (OrderBy es estable).
        Shuffle(deadEnds);

        // Boss: el dead end más lejano.
        var boss = deadEnds
            .Where(r => r.DistanceFromStart >= settings.minBossDistance)
            .OrderByDescending(r => r.DistanceFromStart)
            .FirstOrDefault();

        if (boss == null) return false;

        // Tienda: cualquier otro dead end suficientemente lejos del inicio.
        var shopCandidates = deadEnds
            .Where(r => r != boss && r.DistanceFromStart >= settings.minShopDistance)
            .ToList();

        if (shopCandidates.Count == 0) return false;

        var shop = shopCandidates[rng.Next(shopCandidates.Count)];

        boss.Type = RoomType.Boss;
        shop.Type = RoomType.Shop;
        layout.BossRoom = boss;
        layout.ShopRoom = shop;

        return true;
    }

    // ------------------------------------------------------------------
    // El resto: combate o normal según probabilidad.
    // ------------------------------------------------------------------
    private void AssignRemainingRooms(FloorLayout layout)
    {
        foreach (var room in layout.Rooms.Values)
        {
            if (room.Type != RoomType.None) continue;

            bool forceNormal = settings.safeRoomsNextToStart && room.DistanceFromStart <= 1;

            room.Type = (!forceNormal && rng.NextDouble() < settings.combatChance)
                ? RoomType.Combat
                : RoomType.Normal;
        }
    }

    // ------------------------------------------------------------------
    // Utilidades
    // ------------------------------------------------------------------
    private static bool IsAdjacent(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
