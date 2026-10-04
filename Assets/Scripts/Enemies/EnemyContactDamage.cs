using UnityEngine;

/// <summary>
/// Daño al jugador al tocarlo. Vive en el enemigo (cada prefab define su daño y su cadencia),
/// así el jugador no necesita saber nada de los enemigos.
/// Debe estar en el mismo GameObject que el collider (o que su Rigidbody) para recibir las colisiones.
/// </summary>
public class EnemyContactDamage : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [Tooltip("Tiempo mínimo entre dos daños mientras siguen en contacto (s).")]
    [SerializeField] private float cooldown = 1f;

    private float nextDamageTime;

    private void OnCollisionEnter(Collision collision) => TryDamage(collision);
    private void OnCollisionStay(Collision collision) => TryDamage(collision);

    private void TryDamage(Collision collision)
    {
        if (Time.time < nextDamageTime) return;

        var playerHealth = collision.collider.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null || playerHealth.IsDead) return;

        playerHealth.TakeDamage(damage); // daño directo, sin pong de por medio
        nextDamageTime = Time.time + cooldown;
    }
}
