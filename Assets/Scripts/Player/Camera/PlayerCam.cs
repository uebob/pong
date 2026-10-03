using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cámara en primera persona. Va en el objeto de la cámara (hijo del jugador).
/// - El ratón se lee como desplazamiento por frame, SIN multiplicar por Time.deltaTime
///   (ya es un delta: multiplicarlo haría que la sensibilidad dependiera de los FPS).
/// - Sensibilidad en grados por unidad de ratón, guardada en PlayerPrefs.
/// - Slider opcional del menú de pausa: basta con arrastrarlo al campo, el script lo configura solo.
/// - Si el cursor no está bloqueado (menú de pausa abierto), la cámara no gira.
/// </summary>
public class PlayerCam : MonoBehaviour
{
    private const string SensitivityKey = "MouseSensitivity";

    [Header("Referencias")]
    [Tooltip("Cuerpo del jugador: gira en horizontal (yaw). La cámara solo se inclina (pitch).")]
    public Transform playerBody;
    [Tooltip("Slider de sensibilidad del menú de pausa (opcional).")]
    public Slider sensitivitySlider;

    [Header("Sensibilidad")]
    [Tooltip("Grados que gira por cada unidad de movimiento del ratón.")]
    public float sensitivity = 3f;
    [Tooltip("Rango del slider (mínimo, máximo).")]
    public Vector2 sensitivityRange = new Vector2(0.1f, 10f);
    public bool invertY = false;

    [Header("Límites")]
    [Range(45f, 100f)] public float maxPitch = 100f;

    private float pitch;

    private void Start()
    {
        if (playerBody == null)
            Debug.LogWarning("PlayerCam: falta asignar playerBody, la cámara no podrá girar en horizontal.", this);

        // Sensibilidad guardada de otras sesiones
        sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey, sensitivity),
                                  sensitivityRange.x, sensitivityRange.y);

        if (sensitivitySlider != null)
        {
            sensitivitySlider.wholeNumbers = false;
            sensitivitySlider.minValue = sensitivityRange.x;
            sensitivitySlider.maxValue = sensitivityRange.y;
            sensitivitySlider.SetValueWithoutNotify(sensitivity); // sin disparar el evento
            sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        }

        SetCursorLocked(true);
    }

    private void OnDestroy()
    {
        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.RemoveListener(SetSensitivity);
    }

    private void Update()
    {
        // Menú de pausa abierto: el cursor está libre y no se debe girar
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = Input.GetAxisRaw("Mouse X") * sensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sensitivity;

        pitch += invertY ? mouseY : -mouseY;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        if (playerBody != null)
            playerBody.Rotate(Vector3.up * mouseX);
    }

    /// <summary>Cambia la sensibilidad y la guarda. Se puede enlazar a un slider desde el inspector.</summary>
    public void SetSensitivity(float value)
    {
        sensitivity = Mathf.Clamp(value, sensitivityRange.x, sensitivityRange.y);
        PlayerPrefs.SetFloat(SensitivityKey, sensitivity); // Unity lo escribe a disco al cerrar el juego
    }

    /// <summary>Para el menú de pausa: false al abrirlo, true al cerrarlo.</summary>
    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}