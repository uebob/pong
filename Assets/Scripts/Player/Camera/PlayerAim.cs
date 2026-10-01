using UnityEngine;

/// <summary>
/// Puntería compartida por parry y lanzamiento: dirección de la cámara con ayuda
/// hacia el enemigo actual si está dentro de un cono.
/// </summary>
public class PlayerAim : MonoBehaviour
{
    [SerializeField] private Transform playerCamera;
    [Tooltip("Opcional: fuerza el enemigo al que apuntar. Si es null, se usa el enemigo más cercano (tag Enemy).")]
    [SerializeField] private Transform currentEnemyTarget;

    [Tooltip("Si el enemigo está dentro de este ángulo respecto a la cámara, se aplica ayuda de puntería.")]
    [SerializeField] private float aimAssistAngle = 25f;
    [Range(0f, 1f)]
    [SerializeField] private float aimAssistStrength = 0.6f;

    /// <summary>Enemigo forzado o, si no hay, el más cercano al jugador. Null si no hay enemigos.</summary>
    public Transform EnemyTarget =>
        currentEnemyTarget != null ? currentEnemyTarget : PongTargeting.FindClosestEnemy(transform.position);
    public Vector3 Facing => playerCamera != null ? playerCamera.forward : transform.forward;

    public void SetEnemyTarget(Transform enemy) => currentEnemyTarget = enemy;

    /// <summary>Dirección de salida desde fromPosition, con aim assist.</summary>
    public Vector3 GetAimDirection(Vector3 fromPosition)
    {
        Vector3 aim = Facing;

        Transform enemy = EnemyTarget;
        if (enemy != null)
        {
            Vector3 toEnemy = (enemy.position - fromPosition).normalized;
            if (Vector3.Angle(aim, toEnemy) <= aimAssistAngle)
                aim = Vector3.Slerp(aim, toEnemy, aimAssistStrength);
        }

        return aim.normalized;
    }
}