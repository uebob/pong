using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Componente de prueba: genera un piso y lo dibuja con Gizmos (plano XZ).
/// Ponlo en un GameObject vacío. Clic derecho en el componente > "Generate".
/// </summary>
public class FloorGeneratorDebug : MonoBehaviour
{
    [Header("Generación")]
    public FloorGenerationSettings settings = new FloorGenerationSettings();
    public bool useRandomSeed = true;
    public int seed = 12345;
    public bool generateOnStart = true;

    [Header("Construcción (opcional)")]
    public FloorBuilder builder;
    [Tooltip("Con seed aleatorio: cuántos seeds probar hasta encontrar uno que tus prefabs puedan construir.")]
    public int maxSeedRetries = 50;

    [Header("Debug")]
    public float cellSize = 2f;
    public bool showDistances = true;

    public FloorLayout Layout { get; private set; }

    private void Start()
    {
        if (generateOnStart) Generate();
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        var generator = new FloorGenerator(settings);

        // Si hay builder y seed aleatorio, reintentamos con otros seeds hasta que
        // tu librería de prefabs pueda construir el piso entero.
        int attempts = (useRandomSeed && builder != null) ? maxSeedRetries : 1;
        bool built = false;
        string report = null;

        for (int i = 0; i < attempts; i++)
        {
            if (useRandomSeed)
                seed = Random.Range(int.MinValue, int.MaxValue);

            Layout = generator.Generate(seed);
            if (Layout == null) return;

            if (builder == null) break;

            if (builder.CanBuild(Layout, out report))
            {
                builder.Build(Layout);
                built = true;
                break;
            }
        }

        Debug.Log($"Piso generado. Seed: {seed} | Salas: {Layout.Rooms.Count} | " +
                  $"Boss a distancia {Layout.BossRoom.DistanceFromStart} | " +
                  $"Tienda a distancia {Layout.ShopRoom.DistanceFromStart}");

        if (builder != null && !built)
        {
            builder.Clear();
            Debug.LogError($"No se pudo construir el piso (seed {seed}). {report}");
        }
    }

    private Vector3 ToWorld(Vector2Int gridPos)
    {
        // y del grid -> z del mundo (mapa visto desde arriba)
        return transform.position + new Vector3(gridPos.x, 0f, gridPos.y) * cellSize;
    }

    private static Color ColorFor(RoomType type)
    {
        switch (type)
        {
            case RoomType.Start:  return Color.green;
            case RoomType.Normal: return Color.gray;
            case RoomType.Combat: return new Color(0.9f, 0.4f, 0.2f);
            case RoomType.Shop:   return Color.yellow;
            case RoomType.Boss:   return Color.red;
            default:              return Color.magenta;
        }
    }

    private void OnDrawGizmos()
    {
        if (Layout == null) return;

        foreach (var room in Layout.Rooms.Values)
        {
            Vector3 center = ToWorld(room.Position);

            Gizmos.color = ColorFor(room.Type);
            Gizmos.DrawCube(center, new Vector3(cellSize * 0.8f, 0.2f, cellSize * 0.8f));

            // Conexiones (solo Norte y Este para no dibujarlas dos veces)
            Gizmos.color = Color.white;
            if (room.HasDoor(Direction.North))
                Gizmos.DrawLine(center, ToWorld(room.Position + Direction.North.ToVector()));
            if (room.HasDoor(Direction.East))
                Gizmos.DrawLine(center, ToWorld(room.Position + Direction.East.ToVector()));

#if UNITY_EDITOR
            if (showDistances)
                Handles.Label(center + Vector3.up * 0.5f, $"{room.Type}\nd={room.DistanceFromStart}");
#endif
        }
    }
}