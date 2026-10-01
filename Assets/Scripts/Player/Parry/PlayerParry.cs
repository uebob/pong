using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Parry con ventana temporal y comprobación de que el pong viene de frente.
/// Usa PongDetector (radio) y PlayerAim (dirección con aim assist).
/// Parrear NO cambia el owner del pong; tras la recuperación, el propio pong elige objetivo.
/// </summary>
[RequireComponent(typeof(PongDetector), typeof(PlayerAim))]
public class PlayerParry : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputAction parryAction =
        new InputAction("Parry", InputActionType.Button, "<Keyboard>/f");

    [Header("Parry")]
    [Tooltip("Tiempo que la ventana de parry permanece activa tras pulsar (s).")]
    [SerializeField] private float parryWindow = 0.2f;
    [Tooltip("Tiempo mínimo entre pulsaciones (s).")]
    [SerializeField] private float parryCooldown = 0.4f;
    [Tooltip("Proyectiles máximos por pulsación.")]
    [SerializeField] private int maxProjectilesPerParry = 1;
    [Tooltip("1 = solo justo delante, 0 = hemisferio frontal, -1 = cualquier dirección.")]
    [Range(-1f, 1f)]
    [SerializeField] private float minFacingDot = 0f;

    public event Action ParryStarted;
    public event Action<PongProjectile> ParrySucceeded;

    private PongDetector detector;
    private PlayerAim aim;
    private readonly List<PongProjectile> nearby = new List<PongProjectile>(16);

    private float windowTimer;
    private float cooldownTimer;
    private int parriedThisWindow;

    private void Awake()
    {
        detector = GetComponent<PongDetector>();
        aim = GetComponent<PlayerAim>();
    }

    private void OnEnable() => parryAction.Enable();
    private void OnDisable() => parryAction.Disable();

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
                ParrySucceeded?.Invoke(projectile);
                parriedThisWindow++;

                if (parriedThisWindow >= maxProjectilesPerParry)
                {
                    windowTimer = 0f;
                    projectile.ChangeFaction(PongFaction.Player);
                    break;
                }
            }
        }
    }
}