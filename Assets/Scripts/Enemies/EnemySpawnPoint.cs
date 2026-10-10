using UnityEngine;

/// <summary>
/// Marca dónde aparece un enemigo dentro de una sala de combate.
/// Ponlo en un GameObject vacío hijo del prefab de sala (se rota con la sala).
/// </summary>
public class EnemySpawnPoint : MonoBehaviour
{
    [Tooltip("Enemigos posibles en este punto (se elige uno al azar). " +
             "Si está vacío se usa la lista por defecto de la sala (CombatRoom).")]
    public GameObject[] enemyPrefabs;

    public GameObject PickPrefab(GameObject[] fallback)
    {
        GameObject[] pool = (enemyPrefabs != null && enemyPrefabs.Length > 0) ? enemyPrefabs : fallback;
        if (pool == null || pool.Length == 0) return null;

        return pool[Random.Range(0, pool.Length)];
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        Vector3 p = transform.position + Vector3.up * 0.5f;
        Gizmos.DrawWireSphere(p, 0.5f);
        Gizmos.DrawLine(transform.position, p);
        Gizmos.DrawRay(p, transform.forward * 0.8f); // hacia dónde mirará el enemigo
    }
}
