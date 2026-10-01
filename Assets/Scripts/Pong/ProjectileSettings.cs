using UnityEngine;

/// <summary>
/// Ajustes de "feel" del movimiento del proyectil (curvas y tiempos).
/// Las estadísticas numéricas que los pasivos pueden modificar (velocidad, giro,
/// boost del parry...) ahora viven en PongStat / PongDefinition.baseStats.
/// Crear con: Assets > Create > Pong > Projectile Settings
/// </summary>
[CreateAssetMenu(menuName = "Pong/Projectile Settings", fileName = "ProjectileSettings")]
public class ProjectileSettings : ScriptableObject
{
    [Header("Velocidad")]
    [Tooltip("Con qué rapidez la velocidad se acerca a la objetivo (1/s). Más alto = más brusco.")]
    public float speedSmoothing = 3f;

    [Header("Steering")]
    [Tooltip("Multiplicador de velocidad según alineación con el objetivo. " +
             "X: -1 (objetivo detrás) a 1 (objetivo de frente). Y: factor sobre la velocidad de crucero.")]
    public AnimationCurve slowdownByAlignment = new AnimationCurve(
        new Keyframe(-1f, 0.35f),
        new Keyframe(0f, 0.6f),
        new Keyframe(1f, 1f));

    [Tooltip("Por debajo de esta distancia al objetivo, el giro se multiplica (evita órbitas).")]
    public float closeRange = 4f;
    public float closeRangeTurnMultiplier = 1.75f;

    [Header("Parry")]
    [Tooltip("Pausa breve (hit-stop) del proyectil al ser parreado (s).")]
    public float hitStopDuration = 0.06f;

    [Tooltip("Recuperación tras un parry (s): el pong vuela hacia fuera SIN objetivo ni homing. " +
             "Al terminar elige objetivo: enemigo más cercano o, si no hay, el jugador.")]
    public float homingRecoveryTime = 0.5f;

    [Tooltip("Tiempo que tarda el homing en entrar a pleno tras elegir objetivo (s). 0 = de golpe.")]
    public float homingRampTime = 0.25f;

    [Tooltip("Curva de entrada del homing (X: 0-1 del tiempo, Y: 0-1 de fuerza).")]
    public AnimationCurve homingRampCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Objetivo")]
    [Tooltip("Cada cuánto busca un objetivo nuevo si pierde el actual (s).")]
    public float retargetInterval = 0.5f;

    [Tooltip("Tiempo tras un parry durante el cual no se puede volver a parrear (s).")]
    public float parryLockout = 0.3f;
}