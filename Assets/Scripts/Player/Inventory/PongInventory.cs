using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Cola FIFO de pongs del jugador (instancias desactivadas colgadas de un holster).
/// - E: guarda pongs en rango (solo si el jugador es su owner) al final de la cola.
/// - Click derecho: saca el primero de la cola y lo lanza.
/// Al guardar se pierden parries y velocidad; al lanzar salen a velocidad de crucero.
/// </summary>
[RequireComponent(typeof(PongDetector), typeof(PlayerAim))]
public class PongInventory : MonoBehaviour
{
    [Header("Cola")]
    [SerializeField] private int capacity = 5;
    [Tooltip("Pongs con los que empieza el jugador, en orden de salida.")]
    [SerializeField] private PongDefinition[] startingPongs;

    [Header("Referencias")]
    [Tooltip("Donde se cuelgan los pongs guardados. Si es null, el propio jugador.")]
    [SerializeField] private Transform holster;
    [Tooltip("Punto desde el que se lanzan. Si es null, delante del jugador.")]
    [SerializeField] private Transform launchPoint;
    [SerializeField] private float fallbackLaunchDistance = 1.5f;

    [Header("Guardar")]
    [Tooltip("Pongs que se guardan por pulsación de E (0 = todos los que quepan).")]
    [SerializeField] private int maxStoresPerPress = 1;

    [Header("Input")]
    [SerializeField] private InputAction storeAction =
        new InputAction("Store Pong", InputActionType.Button, "<Keyboard>/e");
    [SerializeField] private InputAction launchAction =
        new InputAction("Launch Pong", InputActionType.Button, "<Mouse>/rightButton");

    /// <summary>Se dispara cuando cambia el contenido de la cola (para la HUD).</summary>
    public event Action Changed;

    public int Count => queue.Count;
    public int Capacity => capacity;
    public IReadOnlyList<PongProjectile> Items => queue;
    public PongProjectile Next => queue.Count > 0 ? queue[0] : null;

    private readonly List<PongProjectile> queue = new List<PongProjectile>();
    private readonly List<PongProjectile> nearby = new List<PongProjectile>(16);

    private PongDetector detector;
    private PlayerAim aim;

    private void Awake()
    {
        detector = GetComponent<PongDetector>();
        aim = GetComponent<PlayerAim>();
        if (holster == null) holster = transform;

        FillStartingPongs();
    }

    private void OnEnable()
    {
        storeAction.Enable();
        launchAction.Enable();
    }

    private void OnDisable()
    {
        storeAction.Disable();
        launchAction.Disable();
    }

    private void Update()
    {
        if (storeAction.WasPressedThisFrame()) StoreNearby();
        if (launchAction.WasPressedThisFrame()) TryLaunchNext();
    }

    private void FillStartingPongs()
    {
        if (startingPongs == null) return;

        foreach (var definition in startingPongs)
        {
            if (definition == null) continue;

            PongProjectile pong = definition.Spawn(
                gameObject, PongFaction.Player, transform.position, transform.rotation);
            if (pong == null) continue;

            if (!TryStore(pong))
                pong.Despawn();
        }
    }

    // ---------------------------------------------------------------------

    private void StoreNearby()
    {
        detector.Query(nearby);

        int stored = 0;
        foreach (var pong in nearby)
        {
            if (!pong.CanBeStoredBy(gameObject)) continue;

            if (!TryStore(pong))
                break; // cola llena: ya se ha avisado por Debug

            stored++;
            if (maxStoresPerPress > 0 && stored >= maxStoresPerPress)
                break;
        }
    }

    /// <summary>Añade el pong al final de la cola, si el jugador es su owner y hay hueco.</summary>
    public bool TryStore(PongProjectile pong)
    {
        if (pong == null || !pong.CanBeStoredBy(gameObject)) return false;

        if (queue.Count >= capacity)
        {
            Debug.Log($"Cola de pongs llena ({capacity}): no se puede guardar.");
            return false;
        }

        pong.Store(holster);
        queue.Add(pong);
        Changed?.Invoke();
        return true;
    }

    /// <summary>Saca el primer pong de la cola (FIFO) y lo lanza.</summary>
    public bool TryLaunchNext()
    {
        // Limpia entradas destruidas por si acaso.
        while (queue.Count > 0 && queue[0] == null)
            queue.RemoveAt(0);

        if (queue.Count == 0)
        {
            Debug.Log("Cola de pongs vacía: no hay nada que lanzar.");
            return false;
        }

        PongProjectile pong = queue[0];
        queue.RemoveAt(0);

        Vector3 origin = launchPoint != null
            ? launchPoint.position
            : transform.position + aim.Facing * fallbackLaunchDistance;

        Vector3 direction = aim.GetAimDirection(origin);
        pong.Launch(origin, direction); // el propio pong elige objetivo

        Changed?.Invoke();
        return true;
    }
}