using System.Collections.Generic;
using UnityEngine;

public enum RoomType
{
    None,   // sin asignar todavía
    Start,
    Normal,
    Combat,
    Shop,
    Boss
}

public enum Direction
{
    North = 0,
    East = 1,
    South = 2,
    West = 3
}

public static class DirectionExtensions
{
    public static readonly Direction[] All =
    {
        Direction.North, Direction.East, Direction.South, Direction.West
    };

    public static Vector2Int ToVector(this Direction d)
    {
        switch (d)
        {
            case Direction.North: return new Vector2Int(0, 1);
            case Direction.East:  return new Vector2Int(1, 0);
            case Direction.South: return new Vector2Int(0, -1);
            default:              return new Vector2Int(-1, 0);
        }
    }

    public static Direction Opposite(this Direction d)
    {
        return (Direction)(((int)d + 2) % 4);
    }
}

/// <summary>Datos puros de una sala (sin GameObjects).</summary>
public class RoomData
{
    public Vector2Int Position;
    public RoomType Type = RoomType.None;
    public int DistanceFromStart;

    // Índice = (int)Direction. true = hay puerta hacia esa dirección.
    public bool[] Doors = new bool[4];

    // Estado de juego (lo guardamos aquí, no en el GameObject)
    public bool Visited;
    public bool Completed;

    public bool HasDoor(Direction d) => Doors[(int)d];

    /// <summary>Puertas como máscara de bits (bit i = Direction i). N=1, E=2, S=4, W=8.</summary>
    public int DoorMask
    {
        get
        {
            int m = 0;
            for (int i = 0; i < Doors.Length; i++)
                if (Doors[i]) m |= 1 << i;
            return m;
        }
    }

    public int DoorCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < Doors.Length; i++)
                if (Doors[i]) n++;
            return n;
        }
    }

    public bool IsDeadEnd => DoorCount == 1;
}

/// <summary>El piso completo: un diccionario posición de grid -> sala.</summary>
public class FloorLayout
{
    public int Seed;
    public Dictionary<Vector2Int, RoomData> Rooms = new Dictionary<Vector2Int, RoomData>();

    public RoomData StartRoom;
    public RoomData BossRoom;
    public RoomData ShopRoom;

    public bool HasRoom(Vector2Int pos) => Rooms.ContainsKey(pos);

    public bool TryGetRoom(Vector2Int pos, out RoomData room) => Rooms.TryGetValue(pos, out room);

    public RoomData AddRoom(Vector2Int pos, RoomType type = RoomType.None)
    {
        var room = new RoomData { Position = pos, Type = type };
        Rooms[pos] = room;
        return room;
    }

    public int CountNeighbors(Vector2Int pos)
    {
        int n = 0;
        foreach (var dir in DirectionExtensions.All)
            if (Rooms.ContainsKey(pos + dir.ToVector()))
                n++;
        return n;
    }

    /// <summary>Las puertas se derivan de las salas vecinas.</summary>
    public void RefreshDoors()
    {
        foreach (var room in Rooms.Values)
        {
            foreach (var dir in DirectionExtensions.All)
                room.Doors[(int)dir] = Rooms.ContainsKey(room.Position + dir.ToVector());
        }
    }

    /// <summary>BFS desde la sala inicial para rellenar DistanceFromStart.</summary>
    public void ComputeDistances()
    {
        var queue = new Queue<RoomData>();
        var visited = new HashSet<Vector2Int>();

        StartRoom.DistanceFromStart = 0;
        queue.Enqueue(StartRoom);
        visited.Add(StartRoom.Position);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var dir in DirectionExtensions.All)
            {
                if (!current.HasDoor(dir)) continue;

                var nextPos = current.Position + dir.ToVector();
                if (visited.Contains(nextPos)) continue;

                var next = Rooms[nextPos];
                next.DistanceFromStart = current.DistanceFromStart + 1;
                visited.Add(nextPos);
                queue.Enqueue(next);
            }
        }
    }
}