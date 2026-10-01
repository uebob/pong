using UnityEngine;

/// <summary>
/// Movimiento con Rigidbody y máquina de estados.
/// Estados: Grounded, Airborne, Dashing, GroundPound, WallCling, Sliding.
/// Requiere un Collider (idealmente cápsula con Physic Material de fricción 0).
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PlayerMovement : MonoBehaviour
{
    public enum State { Grounded, Airborne, Dashing, GroundPound, WallCling, Sliding }

    [Header("Referencias")]
    [Tooltip("Cámara de referencia para el movimiento y el dash. Si está vacío usa Camera.main.")]
    public Transform cameraTransform;
    public Transform groundCheck;

    [Header("Movimiento")]
    public float maxSpeed = 6f;                 // velocidad de carrera "normal"
    public float groundAcceleration = 60f;      // aceleración / frenado en suelo
    public float groundExcessDecel = 30f;       // cuánto frena en suelo si va por encima de maxSpeed (post-dash)
    public float airAcceleration = 30f;         // control en el aire (solo suma velocidad hasta maxSpeed)
    public float airExcessDecel = 10f;          // frenado en el aire del exceso de velocidad (si NO se conserva momentum)

    [Header("Salto")]
    [Tooltip("Saltos disponibles: 0 = no salta, 1 = normal, 2 = doble salto...")]
    public int maxJumps = 2;
    public float jumpHeight = 1.6f;
    public float jumpBufferTime = 0.1f;
    public AudioClip jumpSoundEffect;

    [Header("Detección de suelo")]
    public float groundDistance = 0.3f;
    public LayerMask groundMask;

    [Header("Dash")]
    public KeyCode dashKey = KeyCode.LeftShift;
    public float dashSpeed = 22f;
    public float dashDuration = 0.18f;
    public float dashCooldown = 0.4f;
    public int maxAirDashes = 1;
    public AudioClip dashSoundEffect;

    [Header("Ground Pound")]
    public KeyCode groundPoundKey = KeyCode.LeftControl;
    public float groundPoundSpeed = 35f;
    public float groundPoundHorizontalBrake = 60f;  // cuán rápido se cancela la velocidad horizontal
    public float bounceWindow = 0.5f;               // tiempo tras aterrizar para hacer el bounce
    [Tooltip("Altura del bounce = altura de caída del ground pound x este valor. " +
             "<1 pierdes altura al encadenar, =1 te mantienes, >1 ganas altura.")]
    public float bounceHeightMultiplier = 0.9f;
    [Tooltip("Altura mínima del bounce (para caídas muy cortas).")]
    public float minBounceHeight = 2f;
    [Tooltip("Altura máxima del bounce. 0 = sin límite.")]
    public float maxBounceHeight = 0f;
    public AudioClip startGroundPoundSoundEffect;
    public AudioClip groundPoundSoundEffect;

    [Header("Wall Cling / Wall Jump")]
    public LayerMask wallMask;
    public float wallCheckDistance = 0.15f;
    public float wallSlideSpeed = 2f;
    public bool requireInputTowardWall = true;
    public float wallJumpHorizontalSpeed = 7f;
    public float wallJumpHeight = 1.6f;
    public float wallJumpControlLock = 0.2f;        // tiempo sin control aéreo tras un walljump
    public bool wallJumpRefillsJumps = true;

    [Header("Slide en rampas (tras ground pound)")]
    [Tooltip("Inclinación mínima (grados) para considerar el suelo una rampa.")]
    public float minSlideAngle = 15f;
    [Tooltip("Velocidad mínima con la que empieza el slide.")]
    public float slideMinEntrySpeed = 10f;
    [Tooltip("Multiplica la velocidad de entrada (la que sale de convertir la caída en velocidad a lo largo de la rampa).")]
    public float slideEntrySpeedMultiplier = 1f;
    [Tooltip("Aceleración extra cuesta abajo, además de la gravedad.")]
    public float slideExtraAcceleration = 10f;
    [Tooltip("Por debajo de esta velocidad el slide termina.")]
    public float slideExitSpeed = 2f;

    public State CurrentState => state;

    // --- Internos ---
    private Rigidbody rb;
    private Collider col;
    private State state = State.Airborne;

    private float h, v;
    private Vector3 wishDir;        // dirección de input relativa a la cámara (magnitud 0..1)
    private Vector3 camForward;     // forward de la cámara aplanado
    private float jumpBufferedUntil;
    private bool dashQueued, poundQueued;

    private bool isGrounded;
    private int jumpsRemaining;
    private int airDashesRemaining;
    private bool preserveMomentum;  // true = sin recorte de velocidad en el aire

    private float stateTimer;
    private float lastJumpTime = -10f, lastDashTime = -10f, lastPoundLandTime = -10f, lastWallJumpTime = -10f;
    private float airControlLockedUntil;
    private Vector3 dashVelocity;
    private Vector3 wallNormal;
    private float poundStartY;      // altura a la que empezó el ground pound
    private float pendingBounceHeight;  // altura del bounce calculada al aterrizar
    private Vector3 slideNormal = Vector3.up;
    private AudioSource audioSource;

    private bool JumpPressed => Time.time <= jumpBufferedUntil;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        rb.freezeRotation = true;
        rb.linearDamping = 0f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // el ground pound es rápido
        audioSource = GetComponent<AudioSource>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    // ---------------------------------------------------------------- INPUT
    void Update()
    {
        h = Input.GetAxisRaw("Horizontal");
        v = Input.GetAxisRaw("Vertical");

        if (Input.GetButtonDown("Jump")) jumpBufferedUntil = Time.time + jumpBufferTime;
        if (Input.GetKeyDown(dashKey)) dashQueued = true;
        if (Input.GetKeyDown(groundPoundKey)) poundQueued = true;
    }

    void FixedUpdate()
    {
        UpdateWishDirection();
        isGrounded = CheckGround();

        switch (state)
        {
            case State.Grounded:    TickGrounded();    break;
            case State.Airborne:    TickAirborne();    break;
            case State.Dashing:     TickDashing();     break;
            case State.GroundPound: TickGroundPound(); break;
            case State.WallCling:   TickWallCling();   break;
            case State.Sliding:     TickSliding();     break;
        }

        dashQueued = false;
        poundQueued = false;
    }

    void UpdateWishDirection()
    {
        Transform o = cameraTransform != null ? cameraTransform : transform;
        Vector3 f = Vector3.ProjectOnPlane(o.forward, Vector3.up);
        if (f.sqrMagnitude < 0.001f) f = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        f.Normalize();
        Vector3 r = Vector3.ProjectOnPlane(o.right, Vector3.up).normalized;

        camForward = f;
        wishDir = f * v + r * h;
        if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();
    }

    bool CheckGround()
    {
        // Pequeño bloqueo tras saltar para no "re-detectar" el suelo en el mismo impulso
        if (Time.time - lastJumpTime < 0.1f) return false;
        return Physics.CheckSphere(groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    // ------------------------------------------------------- MÁQUINA DE ESTADOS
    void ChangeState(State next)
    {
        if (next == state) return;
        state = next;
        rb.useGravity = true; // por defecto; los estados que no quieran gravedad la apagan en su Enter
        stateTimer = 0f;

        switch (next)
        {
            case State.Grounded:
                RefreshGroundResources();
                break;

            case State.Dashing:
                Vector3 dir = wishDir.sqrMagnitude > 0.01f ? wishDir.normalized : camForward;
                dashVelocity = dir * dashSpeed;
                stateTimer = dashDuration;
                lastDashTime = Time.time;
                rb.useGravity = false;
                rb.linearVelocity = dashVelocity;
                break;

            case State.GroundPound:
                audioSource.PlayOneShot(startGroundPoundSoundEffect);
                poundStartY = rb.position.y;
                rb.useGravity = false;
                break;

            case State.Sliding:
                RefreshGroundResources(); // estás en el suelo: recuperas saltos y dash
                rb.useGravity = false;    // la gravedad se aplica a mano a lo largo de la rampa
                break;

            case State.WallCling:
                // Anula el movimiento contra la pared
                rb.linearVelocity = new Vector3(0f, Mathf.Min(rb.linearVelocity.y, 0f), 0f);
                break;
        }
    }

    void RefreshGroundResources()
    {
        jumpsRemaining = maxJumps;
        airDashesRemaining = maxAirDashes;
        preserveMomentum = false;
    }

    // ---------------------------------------------------------------- GROUNDED
    void TickGrounded()
    {
        if (!isGrounded)
        {
            // Caminar fuera de un borde cuenta como haber gastado el salto de suelo
            jumpsRemaining = Mathf.Min(jumpsRemaining, Mathf.Max(maxJumps - 1, 0));
            ChangeState(State.Airborne);
            return;
        }

        if (TryStartDash()) return;
        if (JumpPressed && TryJump()) return;

        GroundMove();
    }

    void GroundMove()
    {
        Vector3 flat = FlatVelocity();
        // Si vamos más rápido que maxSpeed (p. ej. tras un dash) frenamos suave; si no, aceleramos normal.
        float rate = flat.magnitude > maxSpeed + 0.01f ? groundExcessDecel : groundAcceleration;
        flat = Vector3.MoveTowards(flat, wishDir * maxSpeed, rate * Time.fixedDeltaTime);
        SetFlatVelocity(flat);
    }

    // ---------------------------------------------------------------- AIRBORNE
    void TickAirborne()
    {
        if (isGrounded) { ChangeState(State.Grounded); return; }
        if (poundQueued) { ChangeState(State.GroundPound); return; }
        if (TryStartDash()) return;

        if (CanWallCling(out Vector3 n))
        {
            wallNormal = n;
            ChangeState(State.WallCling);
            return;
        }

        if (JumpPressed) TryJump(); // salto aéreo (double jump...)

        AirMove();
    }

    void AirMove()
    {
        Vector3 flat = FlatVelocity();

        // Control aéreo estilo "Source": el input solo AÑADE velocidad en su dirección
        // hasta maxSpeed. Nunca recorta la velocidad que ya llevas.
        if (Time.time >= airControlLockedUntil && wishDir.sqrMagnitude > 0.01f)
        {
            float current = Vector3.Dot(flat, wishDir);
            float add = maxSpeed - current;
            if (add > 0f)
                flat += wishDir * Mathf.Min(add, airAcceleration * Time.fixedDeltaTime);
        }

        // Sin momentum conservado, el exceso de velocidad se disipa poco a poco.
        if (!preserveMomentum)
        {
            float s = flat.magnitude;
            if (s > maxSpeed)
                flat = flat.normalized * Mathf.MoveTowards(s, maxSpeed, airExcessDecel * Time.fixedDeltaTime);
        }

        SetFlatVelocity(flat);
    }

    // ------------------------------------------------------------------- SALTO
    bool TryJump()
    {
        if (jumpsRemaining <= 0) return false;
        jumpsRemaining--;

        float height = jumpHeight;
        // Ground pound bounce: salto pulsado poco después de aterrizar de un ground pound
        if (isGrounded && Time.time - lastPoundLandTime <= bounceWindow)
        {
            height = pendingBounceHeight;
            lastPoundLandTime = -10f;
        }

        ApplyJumpVelocity(height);
        ChangeState(State.Airborne);
        return true;
    }

    void ApplyJumpVelocity(float height)
    {
        Vector3 vel = rb.linearVelocity;
        vel.y = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * height);
        rb.linearVelocity = vel;
        lastJumpTime = Time.time;
        jumpBufferedUntil = 0f; // consumir el buffer
        audioSource.PlayOneShot(jumpSoundEffect);
    }

    // -------------------------------------------------------------------- DASH
    bool TryStartDash()
    {
        if (!dashQueued) return false;
        if (Time.time - lastDashTime < dashDuration + dashCooldown) return false;
        if (!isGrounded && airDashesRemaining <= 0) return false;

        audioSource.PlayOneShot(dashSoundEffect);
        if (!isGrounded) airDashesRemaining--;
        ChangeState(State.Dashing);
        return true;
    }

    void TickDashing()
    {
        if (isGrounded) RefreshGroundResources(); // aterrizar durante el dash devuelve saltos

        // Saltar durante el dash: se conserva la velocidad del dash y se desactiva el recorte en el aire
        if (JumpPressed && jumpsRemaining > 0)
        {
            preserveMomentum = true;
            rb.linearVelocity = dashVelocity;
            TryJump();
            return;
        }

        rb.linearVelocity = dashVelocity;

        stateTimer -= Time.fixedDeltaTime;
        if (stateTimer <= 0f)
            ChangeState(isGrounded ? State.Grounded : State.Airborne);
    }

    // ------------------------------------------------------------- GROUND POUND
    void TickGroundPound()
    {
        if (isGrounded)
        {
            Vector3 impactVelocity = rb.linearVelocity;
            rb.linearVelocity = Vector3.zero;
            lastPoundLandTime = Time.time;

            float fallHeight = Mathf.Max(poundStartY - rb.position.y, 0f);
            float bounce = Mathf.Max(fallHeight * bounceHeightMultiplier, minBounceHeight);
            if (maxBounceHeight > 0f) bounce = Mathf.Min(bounce, maxBounceHeight);
            pendingBounceHeight = bounce;

            // ¿Rampa? -> slide en lugar de quedarse parado
            if (GetGroundNormal(out Vector3 n) && Vector3.Angle(n, Vector3.up) >= minSlideAngle)
            {
                slideNormal = n;
                Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, n).normalized;
                // La caída se convierte en velocidad a lo largo de la rampa
                float entry = Vector3.ProjectOnPlane(impactVelocity, n).magnitude * slideEntrySpeedMultiplier;
                rb.linearVelocity = downhill * Mathf.Max(entry, slideMinEntrySpeed);
                ChangeState(State.Sliding);
                return;
            }

            audioSource.PlayOneShot(groundPoundSoundEffect);
            ChangeState(State.Grounded);
            return;
        }

        // Cancela la velocidad horizontal de forma progresiva y cae a velocidad constante
        Vector3 flat = Vector3.MoveTowards(FlatVelocity(), Vector3.zero, groundPoundHorizontalBrake * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(flat.x, -groundPoundSpeed, flat.z);
    }

    // ------------------------------------------------------------------- SLIDING
    void TickSliding()
    {
        // Se acaba la rampa (o saltamos de ella): conservamos la velocidad en el aire
        if (!isGrounded)
        {
            preserveMomentum = true;
            ChangeState(State.Airborne);
            return;
        }

        if (TryStartDash()) return;

        // Saltar durante el slide conserva la velocidad, igual que en el dash
        if (JumpPressed && jumpsRemaining > 0)
        {
            preserveMomentum = true;
            TryJump();
            return;
        }

        if (GetGroundNormal(out Vector3 n)) slideNormal = n;

        Vector3 vel = rb.linearVelocity;
        bool flatGround = Vector3.Angle(slideNormal, Vector3.up) < minSlideAngle;

        if (flatGround || vel.magnitude < slideExitSpeed)
        {
            // Suelo plano: la velocidad se redirige al plano horizontal y GroundMove la frena
            Vector3 flat = new Vector3(vel.x, 0f, vel.z);
            rb.linearVelocity = flat.sqrMagnitude > 0.001f ? flat.normalized * vel.magnitude : Vector3.zero;
            ChangeState(State.Grounded);
            return;
        }

        // Gravedad a lo largo de la rampa + aceleración extra cuesta abajo
        Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, slideNormal).normalized;
        vel += Vector3.ProjectOnPlane(Physics.gravity, slideNormal) * Time.fixedDeltaTime;
        vel += downhill * slideExtraAcceleration * Time.fixedDeltaTime;
        rb.linearVelocity = Vector3.ProjectOnPlane(vel, slideNormal); // pegado a la superficie
    }

    // ---------------------------------------------------------------- WALL CLING
    void TickWallCling()
    {
        if (isGrounded) { ChangeState(State.Grounded); return; }
        if (poundQueued) { ChangeState(State.GroundPound); return; }
        if (TryStartDash()) return;

        if (JumpPressed) { WallJump(); return; }

        if (!DetectWall(out Vector3 n) || (requireInputTowardWall && Vector3.Dot(wishDir, -n) < 0.1f))
        {
            ChangeState(State.Airborne);
            return;
        }
        wallNormal = n;

        // Descenso lento
        Vector3 vel = rb.linearVelocity;
        vel.x = 0f; vel.z = 0f;
        vel.y = Mathf.Max(vel.y, -wallSlideSpeed);
        rb.linearVelocity = vel;
    }

    void WallJump()
    {
        Vector3 vel = wallNormal * wallJumpHorizontalSpeed;
        vel.y = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * wallJumpHeight);
        rb.linearVelocity = vel;

        lastJumpTime = lastWallJumpTime = Time.time;
        airControlLockedUntil = Time.time + wallJumpControlLock;
        jumpBufferedUntil = 0f;
        if (wallJumpRefillsJumps) jumpsRemaining = maxJumps;

        ChangeState(State.Airborne);
    }

    bool CanWallCling(out Vector3 normal)
    {
        normal = Vector3.zero;
        if (rb.linearVelocity.y > 0f) return false;                 // solo al caer
        if (Time.time - lastWallJumpTime < 0.25f) return false;     // no re-engancharse justo tras un walljump
        if (!DetectWall(out normal)) return false;
        if (requireInputTowardWall && Vector3.Dot(wishDir, -normal) < 0.1f) return false;
        return true;
    }

    /// <summary>Lanza 8 rayos horizontales y devuelve la normal de la pared más cercana.</summary>
    bool DetectWall(out Vector3 normal)
    {
        normal = Vector3.zero;
        Bounds b = col.bounds;
        float reach = Mathf.Max(b.extents.x, b.extents.z) + wallCheckDistance;
        float best = float.MaxValue;

        for (int i = 0; i < 8; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
            if (Physics.Raycast(b.center, dir, out RaycastHit hit, reach, wallMask, QueryTriggerInteraction.Ignore)
                && Mathf.Abs(hit.normal.y) < 0.2f && hit.distance < best)
            {
                best = hit.distance;
                normal = hit.normal;
            }
        }
        return best < float.MaxValue;
    }

    // ----------------------------------------------------------------- UTILIDADES
    bool GetGroundNormal(out Vector3 normal)
    {
        normal = Vector3.up;
        Vector3 origin = groundCheck.position + Vector3.up * 0.1f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundDistance + 0.5f, groundMask, QueryTriggerInteraction.Ignore))
        {
            normal = hit.normal;
            return true;
        }
        return false;
    }

    Vector3 FlatVelocity()
    {
        Vector3 vel = rb.linearVelocity;
        return new Vector3(vel.x, 0f, vel.z);
    }

    void SetFlatVelocity(Vector3 flat)
    {
        rb.linearVelocity = new Vector3(flat.x, rb.linearVelocity.y, flat.z);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}