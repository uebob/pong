using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimapa en un Canvas. Va en un Panel de UI (con una Image de fondo).
/// Dibuja el FloorLayout con una Image por sala y una por conexión, y se desplaza
/// para mantener centrada la sala donde está el jugador.
/// Niebla de guerra estilo Isaac: salas visitadas + salas contiguas a visitadas.
/// </summary>
public class MinimapUI : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Opcional. Si está vacío se busca por tag al construir el mapa.")]
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";

    [Header("Aspecto")]
    [SerializeField] private float cellSize = 28f;
    [SerializeField] private float spacing = 8f;
    [SerializeField] private float connectionThickness = 8f;
    [SerializeField] private float currentMarkerPadding = 4f;

    [Header("Visibilidad")]
    [Tooltip("Muestra todo el piso (útil para debug).")]
    [SerializeField] private bool showAllRooms = false;
    [Tooltip("Las salas contiguas a una visitada muestran su color si son tienda o boss.")]
    [SerializeField] private bool revealSpecialRoomsWhenAdjacent = true;

    [Header("Colores")]
    [SerializeField] private Color startColor = new Color(0.3f, 0.85f, 0.3f);
    [SerializeField] private Color normalColor = new Color(0.75f, 0.75f, 0.75f);
    [SerializeField] private Color combatColor = new Color(0.9f, 0.45f, 0.2f);
    [SerializeField] private Color shopColor = new Color(0.95f, 0.85f, 0.2f);
    [SerializeField] private Color bossColor = new Color(0.85f, 0.15f, 0.15f);
    [SerializeField] private Color unknownColor = new Color(0.35f, 0.35f, 0.35f);
    [SerializeField] private Color connectionColor = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private Color currentMarkerColor = Color.white;

    private enum CellState { Hidden, Seen, Visited }

    private class Link
    {
        public Image image;
        public RoomData a;
        public RoomData b;
    }

    private RectTransform content;
    private FloorLayout layout;
    private FloorBuilder builder;

    private readonly Dictionary<Vector2Int, Image> cells = new Dictionary<Vector2Int, Image>();
    private readonly List<Link> links = new List<Link>();
    private Image marker;

    private Vector2Int currentCell;
    private bool hasCurrent;
    private PlayerPassives passives;
    private bool RevealAll => showAllRooms || (passives != null && passives.Apply(PlayerStat.RevealMap, 0f) > 0f);

    // ------------------------------------------------------------------
    // API pública
    // ------------------------------------------------------------------

    /// <summary>Construye el minimapa para un piso. 'builder' sirve para saber en qué sala está el jugador.</summary>
    public void Build(FloorLayout newLayout, FloorBuilder newBuilder)
    {
        EnsureContent();
        Clear();

        layout = newLayout;
        builder = newBuilder;

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) player = p.transform;
        }

        BindPassives();

        // Orden de creación = orden de dibujado: marcador (fondo), conexiones, salas.
        float markerSize = cellSize + currentMarkerPadding * 2f;
        marker = CreateImage("CurrentMarker", Vector2.zero, new Vector2(markerSize, markerSize), currentMarkerColor);

        foreach (var room in layout.Rooms.Values)
        {
            foreach (var dir in new[] { Direction.North, Direction.East })
            {
                if (!room.HasDoor(dir)) continue;
                if (!layout.Rooms.TryGetValue(room.Position + dir.ToVector(), out var other)) continue;

                Vector2 mid = (CellToUI(room.Position) + CellToUI(other.Position)) * 0.5f;
                float length = spacing + 2f; // un poco más largo: queda tapado por las salas, sin huecos
                Vector2 size = dir == Direction.East
                    ? new Vector2(length, connectionThickness)
                    : new Vector2(connectionThickness, length);

                var img = CreateImage("Link", mid, size, connectionColor);
                links.Add(new Link { image = img, a = room, b = other });
            }
        }

        foreach (var room in layout.Rooms.Values)
        {
            var img = CreateImage($"Cell_{room.Position.x}_{room.Position.y}",
                                  CellToUI(room.Position),
                                  new Vector2(cellSize, cellSize),
                                  unknownColor);
            cells[room.Position] = img;
        }

        hasCurrent = false;
        SetCurrent(layout.StartRoom.Position);
    }

    public void SetShowAll(bool value)
    {
        showAllRooms = value;
        Refresh();
    }

    // ------------------------------------------------------------------
    // Internos
    // ------------------------------------------------------------------

    private void Update()
    {
        if (layout == null || builder == null || player == null) return;

        Vector2Int cell = builder.WorldToGrid(player.position);
        if (hasCurrent && cell == currentCell) return;

        if (layout.Rooms.ContainsKey(cell))
            SetCurrent(cell);
    }

    private void SetCurrent(Vector2Int cell)
    {
        currentCell = cell;
        hasCurrent = true;

        layout.Rooms[cell].Visited = true;

        Vector2 uiPos = CellToUI(cell);
        content.anchoredPosition = -uiPos;             // el mapa se desplaza, la sala actual queda centrada
        marker.rectTransform.anchoredPosition = uiPos;

        Refresh();
    }

    private void Refresh()
    {
        if (layout == null || marker == null) return;

        foreach (var pair in cells)
        {
            RoomData room = layout.Rooms[pair.Key];
            CellState state = GetState(room);

            pair.Value.gameObject.SetActive(state != CellState.Hidden);
            pair.Value.color = ColorFor(room, state);
        }

        foreach (var link in links)
        {
            CellState sa = GetState(link.a);
            CellState sb = GetState(link.b);

            bool visible = sa != CellState.Hidden && sb != CellState.Hidden &&
                           (sa == CellState.Visited || sb == CellState.Visited);

            link.image.gameObject.SetActive(visible);
        }

        marker.gameObject.SetActive(hasCurrent);
    }

    private CellState GetState(RoomData room)
    {
        if (RevealAll || room.Visited) return CellState.Visited;

        foreach (var dir in DirectionExtensions.All)
        {
            if (!room.HasDoor(dir)) continue;
            if (layout.Rooms.TryGetValue(room.Position + dir.ToVector(), out var neighbor) && neighbor.Visited)
                return CellState.Seen;
        }

        return CellState.Hidden;
    }

    private Color ColorFor(RoomData room, CellState state)
    {
        if (state == CellState.Visited) return TypeColor(room.Type);

        bool special = room.Type == RoomType.Shop || room.Type == RoomType.Boss;
        return (revealSpecialRoomsWhenAdjacent && special) ? TypeColor(room.Type) : unknownColor;
    }

    private Color TypeColor(RoomType type)
    {
        switch (type)
        {
            case RoomType.Start:  return startColor;
            case RoomType.Normal: return normalColor;
            case RoomType.Combat: return combatColor;
            case RoomType.Shop:   return shopColor;
            case RoomType.Boss:   return bossColor;
            default:              return unknownColor;
        }
    }

    private Vector2 CellToUI(Vector2Int cell)
    {
        return new Vector2(cell.x, cell.y) * (cellSize + spacing);
    }

    private void EnsureContent()
    {
        if (content != null) return;

        // Recorta lo que se sale del panel
        if (GetComponent<RectMask2D>() == null)
            gameObject.AddComponent<RectMask2D>();

        var go = new GameObject("MinimapContent", typeof(RectTransform));
        content = (RectTransform)go.transform;
        content.SetParent(transform, false);
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
    }

    private void Clear()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            var child = content.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }

        cells.Clear();
        links.Clear();
        marker = null;
        layout = null;
    }

    private Image CreateImage(string objName, Vector2 anchoredPos, Vector2 size, Color color)
    {
        var go = new GameObject(objName, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(content, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private void OnValidate()
    {
        // Permite cambiar showAllRooms desde el Inspector durante el juego
        if (Application.isPlaying && layout != null) Refresh();
    }

    private void BindPassives()
    {
        var found = player != null ? player.GetComponentInParent<PlayerPassives>() : null;
        if (found == passives) return;
        if (passives != null) passives.Changed -= Refresh;
        passives = found;
        if (passives != null) passives.Changed += Refresh; 
    }

    private void OnDestroy()
    {
        if (passives != null) passives.Changed -= Refresh;
    }
}
