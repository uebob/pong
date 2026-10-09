using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Recorre un FloorLayout (datos) e instancia, para cada sala, un prefab de la
/// librería cuyas puertas encajen (rotándolo si hace falta).
/// </summary>
public class FloorBuilder : MonoBehaviour
{
    [SerializeField] private RoomLibrary library;

    [Tooltip("Distancia entre centros de salas contiguas (tamaño de la sala).")]
    [SerializeField] private float roomSize = 20f;

    private Transform floorRoot;
    private readonly Dictionary<Vector2Int, Room> rooms = new Dictionary<Vector2Int, Room>();
    private readonly List<Room> matchBuffer = new List<Room>();
    private readonly List<int> stepBuffer = new List<int>(4);

    public IReadOnlyDictionary<Vector2Int, Room> Rooms => rooms;

    /// <summary>
    /// Comprueba que la librería tiene prefab para TODAS las salas del layout.
    /// Si no, 'report' explica qué combinaciones (tipo + puertas) faltan.
    /// </summary>
    public bool CanBuild(FloorLayout layout, out string report)
    {
        var missing = new HashSet<string>();

        foreach (var data in layout.Rooms.Values)
        {
            library.GetMatchingPrefabs(data.Type, data.DoorMask, matchBuffer);
            if (matchBuffer.Count == 0)
                missing.Add($"{data.Type} con puertas {Room.DescribeMask(data.DoorMask)}");
        }

        if (missing.Count == 0)
        {
            report = null;
            return true;
        }

        var sb = new StringBuilder("Faltan prefabs para: ");
        sb.Append(string.Join(" | ", missing));
        report = sb.ToString();
        return false;
    }

    public void Build(FloorLayout layout)
    {
        Clear();

        floorRoot = new GameObject("Floor").transform;
        floorRoot.SetParent(transform, false);

        // Random propio y determinista (mismo seed = mismas salas elegidas)
        var rng = new System.Random(layout.Seed);

        foreach (var data in layout.Rooms.Values)
        {
            int mask = data.DoorMask;
            library.GetMatchingPrefabs(data.Type, mask, matchBuffer);

            if (matchBuffer.Count == 0)
            {
                Debug.LogError($"[FloorBuilder] Sin prefab para {data.Type} con puertas {Room.DescribeMask(mask)} en {data.Position}.");
                continue;
            }

            Room prefab = matchBuffer[rng.Next(matchBuffer.Count)];
            prefab.GetValidRotations(mask, stepBuffer);
            int steps = stepBuffer[rng.Next(stepBuffer.Count)];

            Vector3 worldPos = new Vector3(data.Position.x, 0f, data.Position.y) * roomSize;
            Quaternion rot = Quaternion.Euler(0f, 90f * steps, 0f);

            Room room = Instantiate(prefab, worldPos, rot, floorRoot);
            room.name = $"Room_{data.Type}_{data.Position.x}_{data.Position.y}";
            room.Initialize(data, steps);

            rooms[data.Position] = room;
        }
    }

    public void Clear()
    {
        rooms.Clear();

        if (floorRoot != null)
        {
            if (Application.isPlaying) Destroy(floorRoot.gameObject);
            else DestroyImmediate(floorRoot.gameObject);
            floorRoot = null;
        }
    }
}
