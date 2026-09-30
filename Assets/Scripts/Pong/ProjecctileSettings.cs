using UnityEngine;

/// <summary>
/// Todos los parámetros de "feel" del proyectil en un asset.
/// Crear con: Assets > Create > Prototype > Projectile Settings
/// </summary>
[CreateAssetMenu(menuName = "Prototype/Projectile Settings", fileName = "ProjectileSettings")]
public class ProjectileSettings : ScriptableObject
{
    [Header("Velocidad")]
    [Tooltip("Velocidad de crucero base (m/s).")]
    public float cruiseSpeed = 12f;

    [Tooltip("Cuánto sube la velocidad de crucero con cada parry (m/s).")]
    public float speedGainPerParry = 2f;

    [Tooltip("Tope de la velocidad de crucero.")]
    public float maxCruiseSpeed = 30f;

    [Tooltip("Con qué rapidez la velocidad se acerca a la objetivo (1/s). Más alto = más brusco.")]
    public float speedSmoothing = 3f;

    [Header("Steering")]
    [Tooltip("Velocidad máxima de giro a pleno homing (grados/s).")]
    public float turnRate = 200f;

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
    [Tooltip("Velocidad de salida = velocidad de crucero * este multiplicador.")]
    public float parryBoostMultiplier = 2f;

    [Tooltip("Pausa breve (hit-stop) del proyectil al ser parreado (s).")]
    public float hitStopDuration = 0.06f;

    [Tooltip("Tiempo que tarda el homing en recuperarse tras un parry (s).")]
    public float homingRecoveryTime = 0.5f;

    [Tooltip("Curva de recuperación del homing (X: 0-1 del tiempo, Y: 0-1 de fuerza).")]
    public AnimationCurve homingRecoveryCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Tiempo tras un parry durante el cual no se puede volver a parrear (s).")]
    public float parryLockout = 0.3f;
}