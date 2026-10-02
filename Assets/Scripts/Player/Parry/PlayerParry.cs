using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Parry con ventana temporal y comprobación de que el pong viene de frente.
/// Usa PongDetector (radio) y PlayerAim (dirección con aim assist).
/// Parrear NO cambia el owner del pong; tras la recuperación, el propio pong elige objetivo.
/// Al parrear el jugador se congela el juego unos instantes (freeze frames).
/// </summary>
[RequireComponent(typeof(PongDetector), typeof(PlayerAim))]
public class PlayerParry : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputAction parryAction =
        new InputAction("Parry", InputActionType.Button, "<Keyboard>/f");

    [Header("Parry")]
    [Tooltip("Tiempo que la ventana de parry permanece activa tras pulsar (s).")]
    [SerializeField] public float parryWindow = 0.2f;
    [Tooltip("Tiempo mínimo entre pulsaciones (s).")]
    [SerializeField] private float parryCooldown = 0.4f;
    [Tooltip("Proyectiles máximos por pulsación.")]
    [SerializeField] private int maxProjectilesPerParry = 1;
    [Tooltip("1 = solo justo delante, 0 = hemisferio frontal, -1 = cualquier dirección.")]
    [Range(-1f, 1f)]
    [SerializeField] private float minFacingDot = 0f;

    [Header("Freeze frames")]
    [Tooltip("Duración del congelado al parrear, en tiempo real (s). 0 = desactivado.")]
    [SerializeField] private float freezeDuration = 0.06f;
    [Tooltip("Time.timeScale durante el freeze. 0 = congelado total; un valor pequeño (0.05) deja un movimiento mínimo.")]
    [Range(0f, 1f)]
    [SerializeField] private float freezeTimeScale = 0f;
    private AudioSource audioSource;
    public AudioClip parrySoundEffect;

    public event Action ParryStarted;
    public event Action<PongProjectile> ParrySucceeded;

    private PongDetector detector;
    private PlayerAim aim;
    private readonly List<PongProjectile> nearby = new List<PongProjectile>(16);

    private float windowTimer;
    private float cooldownTimer;
    private int parriedThisWindow;

    // Freeze frames
    private Coroutine freezeRoutine;
    private float freezeEndTime;
    private float timeScaleBeforeFreeze = 1f;

    private void Awake()
    {
        detector = GetComponent<PongDetector>();
        aim = GetComponent<PlayerAim>();
        audioSource = gameObject.GetComponent<AudioSource>();
    }

    private void OnEnable() => parryAction.Enable();

    private void OnDisable()
    {
        parryAction.Disable();
        CancelFreeze(); // si no, un freeze a medias dejaría el juego parado
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        cooldownTimer -= dt;

        if (parryAction.WasPressedThisFrame() && cooldownTimer <= 0f)
        {
            windowTimer = parryWindow;
            cooldownTimer = parryCooldown;
            parriedThisWindow = 0;
            ParryStarted?.Invoke();
        }

        if (windowTimer > 0f)
        {
            windowTimer -= dt;
            TryParry();
        }
    }

    private void TryParry()
    {
        detector.Query(nearby);
        Vector3 facing = aim.Facing;

        foreach (var projectile in nearby)
        {
            if (!projectile.CanBeParried) continue;

            // Debe estar delante del jugador.
            Vector3 toProjectile = (projectile.transform.position - transform.position).normalized;
            if (Vector3.Dot(facing, toProjectile) < minFacingDot) continue;

            Vector3 aimDirection = aim.GetAimDirection(projectile.transform.position);

            if (projectile.Parry(aimDirection))
            {
                projectile.ChangeFaction(PongFaction.Player);

                ParrySucceeded?.Invoke(projectile);
                audioSource.PlayOneShot(parrySoundEffect);
                RequestFreeze();
                parriedThisWindow++;

                if (parriedThisWindow >= maxProjectilesPerParry)
                {
                    windowTimer = 0f;
                    break;
                }
            }
        }
    }

    // =====================================================================
    // Freeze frames
    // =====================================================================

    /// <summary>
    /// Congela el juego durante freezeDuration (tiempo real). Si ya hay un freeze activo
    /// no se apilan: solo se extiende el final si hace falta.
    /// </summary>
    private void RequestFreeze()
    {
        if (freezeDuration <= 0f) return;

        freezeEndTime = Mathf.Max(freezeEndTime, Time.unscaledTime + freezeDuration);

        if (freezeRoutine == null)
            freezeRoutine = StartCoroutine(FreezeRoutine());
    }

    private IEnumerator FreezeRoutine()
    {
        timeScaleBeforeFreeze = Time.timeScale;
        Time.timeScale = freezeTimeScale;

        // Tiempo real: con timeScale = 0 el tiempo escalado no avanza.
        while (Time.unscaledTime < freezeEndTime)
            yield return null;

        Time.timeScale = timeScaleBeforeFreeze;
        freezeRoutine = null;
    }

    private void CancelFreeze()
    {
        if (freezeRoutine == null) return;

        StopCoroutine(freezeRoutine);
        freezeRoutine = null;
        Time.timeScale = timeScaleBeforeFreeze;
    }
}