using UnityEngine;

/// <summary>
/// Persecución HORIZONTAL del jugador. Solo escribe la velocidad en X/Z y respeta la Y,
/// así que sirve igual para un enemigo terrestre (la gravedad hace el resto) que para uno volador
/// (combinándolo con EnemyHover, que controla únicamente la altura).
/// No evita obstáculos: para niveles con recovecos conviene un NavMeshAgent.
/// Consejo: collider con Physic Material de fricción 0 y una masa alta (así los pongs no lo empujan).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public class EnemyFollowPlayer : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("Distancia a la que detecta al jugador. 0 = siempre lo detecta.")]
    [SerializeField] private float aggroRange = 0f;
    [Tooltip("Velocidad de giro hacia el jugador (grados/s).")]
    [SerializeField] private float turnSpeed = 360f;

    [Header("Persecución")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float acceleration = 20f;
    [Tooltip("Se detiene a esta distancia horizontal del jugador.")]
    [SerializeField] private float stopDistance = 1.5f;

    private Rigidbody rb;
    private Transform player;
    private float nextFindTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        // El jugador puede no existir aún o haber muerto (se destruye): se vuelve a buscar sin saturar la CPU
        if (player == null && Time.time >= nextFindTime)
        {
            nextFindTime = Time.time + 0.5f;
            player = PongTargeting.FindPlayer();
        }

        Vector3 vel = rb.linearVelocity;
        Vector3 flat = new Vector3(vel.x, 0f, vel.z);
        Vector3 desired = Vector3.zero;

        if (player != null)
        {
            Vector3 toPlayer = player.position - rb.position;
            toPlayer.y = 0f;

            bool inRange = aggroRange <= 0f || toPlayer.sqrMagnitude <= aggroRange * aggroRange;
            if (inRange)
            {
                FaceTowards(toPlayer);
                if (toPlayer.magnitude > stopDistance)
                    desired = toPlayer.normalized * moveSpeed;
            }
        }

        // Acelera y frena de forma suave. La velocidad vertical no se toca.
        flat = Vector3.MoveTowards(flat, desired, acceleration * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(flat.x, vel.y, flat.z);
    }

    private void FaceTowards(Vector3 flatDirection)
    {
        if (flatDirection.sqrMagnitude < 0.0001f) return;

        Quaternion goal = Quaternion.LookRotation(flatDirection, Vector3.up);
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, goal, turnSpeed * Time.fixedDeltaTime));
    }
}