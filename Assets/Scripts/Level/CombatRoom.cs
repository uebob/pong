using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Comportamiento de sala de combate (y de boss). Va en la raíz del prefab, junto a Room.
/// Flujo: el jugador entra al trigger -> se cierran las puertas -> aparecen los enemigos
/// de los EnemySpawnPoint -> cuando no queda ninguno, se abren las puertas.
/// </summary>
[RequireComponent(typeof(Room))]
[RequireComponent(typeof(BoxCollider))]
public class CombatRoom : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Segundos entre cerrar las puertas y que aparezcan los enemigos.")]
    [SerializeField] private float spawnDelay = 1f;

    [Tooltip("Enemigos por defecto para los spawn points que no tengan lista propia.")]
    [SerializeField] private GameObject[] defaultEnemyPrefabs;

    /// <summary>Se dispara al limpiar la sala (útil luego para recompensas, minimapa, etc.).</summary>
    public event Action<CombatRoom> Cleared;

    private Room room;
    private readonly List<GameObject> aliveEnemies = new List<GameObject>();
    private bool started;

    private void Awake()
    {
        room = GetComponent<Room>();
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (started) return;
        if (!other.CompareTag(playerTag)) return;
        if (room.Data != null && room.Data.Completed) return;

        StartCoroutine(EncounterRoutine());
    }

    private IEnumerator EncounterRoutine()
    {
        started = true;
        if (room.Data != null) room.Data.Visited = true;

        room.SetDoorsOpen(false);
        yield return new WaitForSeconds(spawnDelay);

        SpawnEnemies();

        if (aliveEnemies.Count == 0)
        {
            Debug.LogWarning($"[CombatRoom] {name}: no se ha generado ningún enemigo " +
                             "(¿faltan EnemySpawnPoint o prefabs?). Se abre la sala para no bloquear al jugador.");
            Complete();
            yield break;
        }

        var wait = new WaitForSeconds(0.25f);
        while (true)
        {
            // Un enemigo cuenta como derrotado si se ha destruido o desactivado
            aliveEnemies.RemoveAll(e => e == null || !e.activeInHierarchy);
            if (aliveEnemies.Count == 0) break;
            yield return wait;
        }

        Complete();
    }

    private void SpawnEnemies()
    {
        foreach (var point in GetComponentsInChildren<EnemySpawnPoint>())
        {
            GameObject prefab = point.PickPrefab(defaultEnemyPrefabs);
            if (prefab == null) continue;

            GameObject enemy = Instantiate(prefab, point.transform.position, point.transform.rotation, transform);
            aliveEnemies.Add(enemy);
        }
    }

    private void Complete()
    {
        if (room.Data != null) room.Data.Completed = true;

        room.SetDoorsOpen(true);
        Cleared?.Invoke(this);
    }
}
