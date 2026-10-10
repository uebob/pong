using UnityEngine;

/// <summary>
/// Puerta tipo garaje. El 'panel' debe estar colocado en su posición CERRADA en el prefab;
/// al abrirse sube 'openHeight' unidades en Y local.
/// (Versión mínima: luego puedes añadirle sonido, partículas, estado bloqueada, etc.)
/// </summary>
public class Door : MonoBehaviour
{
    [SerializeField] private Transform panel;
    [SerializeField] private float openHeight = 6f;
    [SerializeField] private float speed = 6f;

    private Vector3 closedPos;
    private Vector3 openPos;
    private Vector3 target;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (panel == null) panel = transform;

        closedPos = panel.localPosition;
        openPos = closedPos + Vector3.up * openHeight;

        // Por defecto las puertas empiezan abiertas
        IsOpen = true;
        target = openPos;
        panel.localPosition = openPos;
    }

    private void Update()
    {
        panel.localPosition = Vector3.MoveTowards(panel.localPosition, target, speed * Time.deltaTime);
    }

    public void Open()
    {
        IsOpen = true;
        target = openPos;
    }

    public void Close()
    {
        IsOpen = false;
        target = closedPos;
    }
}
