using UnityEngine;

/// <summary>
/// Búsqueda de objetivos para los pongs. Toda la lógica de "a quién persigo" vive aquí,
/// así que si algún día cambias de tags a un registro de enemigos (mejor rendimiento con
/// muchos enemigos), solo hay que tocar este archivo.
///
/// REQUISITO: los tags "Enemy" y "Player" deben existir en Project Settings > Tags and Layers,
/// y estar puestos en la raíz del enemigo y del jugador. Si un tag no está definido,
/// Unity lanza una excepción al buscarlo.
///
/// Las búsquedas por tag son baratas para el uso que se les da (al lanzar, al terminar la
/// recuperación del parry y si se pierde el objetivo), pero no las llames cada frame.
/// </summary>
public static class PongTargeting
{
    public const string EnemyTag = "Enemy";
    public const string PlayerTag = "Player";

    private static Transform cachedPlayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => cachedPlayer = null;

    /// <summary>El jugador (cacheado). Null si no hay ninguno.</summary>
    public static Transform FindPlayer()
    {
        if (cachedPlayer == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(PlayerTag);
            cachedPlayer = player != null ? player.transform : null;
        }

        return cachedPlayer;
    }

    /// <summary>El enemigo activo más cercano a "from". Null si no hay ninguno.</summary>
    public static Transform FindClosestEnemy(Vector3 from)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(EnemyTag);

        Transform closest = null;
        float bestSqrDistance = float.MaxValue;

        foreach (GameObject enemy in enemies)
        {
            float sqrDistance = (enemy.transform.position - from).sqrMagnitude;
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                closest = enemy.transform;
            }
        }

        return closest;
    }

    /// <summary>Regla general: el enemigo más cercano; si no hay ninguno, el jugador.</summary>
    public static Transform FindDefaultTarget(Vector3 from)
    {
        Transform enemy = FindClosestEnemy(from);
        return enemy != null ? enemy : FindPlayer();
    }
}
