using UnityEngine;

/// <summary>
/// Hace que un objeto rote sobre sí mismo y levite suavemente (bob arriba y abajo).
/// Ponlo en el hijo "visual" de un pickup, NO en la raíz: así el trigger no se mueve con la
/// animación. Sirve para cualquier objeto decorativo (items, monedas...).
/// </summary>
public class FloatingSpin : MonoBehaviour
{
    [Header("Giro")]
    [Tooltip("Grados por segundo sobre el eje vertical. 0 = sin giro.")]
    [SerializeField] private float spinSpeed = 90f;

    [Header("Levitación")]
    [Tooltip("Cuánto sube y baja desde su posición inicial (m).")]
    [SerializeField] private float bobAmplitude = 0.1f;
    [Tooltip("Ciclos completos de subida y bajada por segundo.")]
    [SerializeField] private float bobFrequency = 0.5f;
    [Tooltip("Cada objeto empieza en un punto distinto del ciclo, para que varios no se muevan sincronizados.")]
    [SerializeField] private bool randomizePhase = true;

    private Vector3 startLocalPosition;
    private float phase;

    private void Awake()
    {
        startLocalPosition = transform.localPosition;
        phase = randomizePhase ? Random.value * Mathf.PI * 2f : 0f;
    }

    private void Update()
    {
        if (spinSpeed != 0f)
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

        if (bobAmplitude != 0f)
        {
            // Se calcula desde Time.time (no se acumula), así que no se desvía con el tiempo.
            float offset = Mathf.Sin(Time.time * Mathf.PI * 2f * bobFrequency + phase) * bobAmplitude;
            transform.localPosition = startLocalPosition + Vector3.up * offset;
        }
    }
}
