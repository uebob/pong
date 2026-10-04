using UnityEngine;

/// <summary>
/// Flotación VERTICAL: apaga la gravedad y controla la altura del enemigo. Solo escribe la velocidad
/// en Y y respeta X/Z, así que se puede combinar con EnemyPlayerFollow (que controla el plano horizontal).
///
/// Altura objetivo:
///  1. Mantiene una distancia constante al suelo que tiene debajo (sube por rampas y baja por pendientes).
///     Si no hay suelo debajo, conserva la última altura hasta volver a encontrarlo.
///  2. Si el jugador está más alto que esa altura, sube hacia él (nunca baja por debajo de la del suelo).
///  3. Sobre todo eso, un vaivén suave.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public class EnemyHover : MonoBehaviour
{
    [Header("Suelo")]
    [Tooltip("Capas que cuentan como suelo. Pon solo el suelo/terreno, NO al jugador ni a otros enemigos.")]
    [SerializeField] private LayerMask groundMask;
    [Tooltip("Distancia constante al suelo (m).")]
    [SerializeField] private float hoverHeight = 2.5f;
    [Tooltip("Hasta dónde mira hacia abajo en busca de suelo (m).")]
    [SerializeField] private float groundCheckDistance = 30f;

    [Header("Jugador")]
    [Tooltip("Si el jugador está más alto que el enemigo, este sube hacia él.")]
    [SerializeField] private bool followPlayerHeight = true;
    [Tooltip("Altura mínima respecto al jugador cuando éste está por encima (m). Ajusta según el pivote del jugador.")]
    [SerializeField] private float playerHeightOffset = 0.5f;
    [Tooltip("Solo tiene en cuenta al jugador si está a menos de esta distancia horizontal. 0 = sin límite.")]
    [SerializeField] private float playerHeightRange = 0f;

    [Header("Vaivén")]
    [Tooltip("Cuánto sube y baja (m).")]
    [SerializeField] private float bobAmplitude = 0.3f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Respuesta vertical")]
    [Tooltip("Cuán rápido corrige su altura hacia la objetivo.")]
    [SerializeField] private float verticalStiffness = 6f;
    [SerializeField] private float maxVerticalSpeed = 5f;

    private Rigidbody rb;
    private Transform player;
    private float nextFindTime;

    private float floatY;   // última altura de flotación conocida (suelo + hoverHeight)
    private float bobPhase;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.useGravity = false;

        bobPhase = Random.Range(0f, Mathf.PI * 2f); // para que varios enemigos no floten sincronizados
    }

    private void Start()
    {
        floatY = rb.position.y; // sin suelo debajo, se queda a la altura a la que apareció

        if (groundMask.value == 0)
            Debug.LogWarning("EnemyHover: groundMask vacío, nunca detectará suelo.", this);
    }

    private void FixedUpdate()
    {
        // El jugador puede no existir aún o haber muerto (se destruye): se vuelve a buscar sin saturar la CPU
        if (followPlayerHeight && player == null && Time.time >= nextFindTime)
        {
            nextFindTime = Time.time + 0.5f;
            player = PongTargeting.FindPlayer();
        }

        // 1) Suelo: distancia constante. Si no hay suelo, floatY conserva el último valor.
        if (Physics.Raycast(rb.position, Vector3.down, out RaycastHit hit, groundCheckDistance,
                            groundMask, QueryTriggerInteraction.Ignore))
        {
            floatY = hit.point.y + hoverHeight;
        }

        float desiredY = floatY;

        // 2) Jugador por encima: sube hacia él
        if (followPlayerHeight && player != null && PlayerInHeightRange())
            desiredY = Mathf.Max(desiredY, player.position.y + playerHeightOffset);

        // 3) Vaivén
        desiredY += Mathf.Sin(Time.time * bobSpeed + bobPhase) * bobAmplitude;

        float vy = Mathf.Clamp((desiredY - rb.position.y) * verticalStiffness, -maxVerticalSpeed, maxVerticalSpeed);

        Vector3 vel = rb.linearVelocity;
        rb.linearVelocity = new Vector3(vel.x, vy, vel.z); // X/Z no se tocan
    }

    private bool PlayerInHeightRange()
    {
        if (playerHeightRange <= 0f) return true;

        Vector3 flat = player.position - rb.position;
        flat.y = 0f;
        return flat.sqrMagnitude <= playerHeightRange * playerHeightRange;
    }
}