using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// Volumen controlado por un slider del menú de pausa, guardado en PlayerPrefs.
/// - Sin AudioMixer: controla el volumen global (AudioListener.volume).
/// - Con AudioMixer: controla un parámetro expuesto (p. ej. "MasterVolume", "MusicVolume", "SfxVolume").
/// Se puede usar varias veces (una instancia por slider).
/// IMPORTANTE: ponlo en un objeto que esté activo al empezar el juego (no dentro del menú de pausa
/// desactivado), para que el volumen guardado se aplique desde el inicio.
/// </summary>
public class VolumeSettings : MonoBehaviour
{
    [Header("UI")]
    public Slider slider;

    [Header("Audio")]
    [Tooltip("Opcional. Si está vacío se usa el volumen global (AudioListener.volume).")]
    public AudioMixer mixer;
    [Tooltip("Nombre del parámetro expuesto en el mixer (clic derecho en Volume > Expose).")]
    public string exposedParameter = "MasterVolume";

    [Header("Guardado")]
    [Tooltip("Clave de PlayerPrefs. Usa una distinta para cada instancia.")]
    public string prefsKey = "MasterVolume";
    [Range(0.0001f, 1f)] public float defaultVolume = 1f;

    private const float MinVolume = 0.0001f; // evita Log10(0)

    private float volume;

    private void Start()
    {
        volume = Mathf.Clamp(PlayerPrefs.GetFloat(prefsKey, defaultVolume), MinVolume, 1f);
        Apply(volume);

        if (slider != null)
        {
            slider.wholeNumbers = false;
            slider.minValue = MinVolume;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(volume);
            slider.onValueChanged.AddListener(SetVolume);
        }
    }

    private void OnDestroy()
    {
        if (slider != null)
            slider.onValueChanged.RemoveListener(SetVolume);
    }

    /// <summary>Volumen lineal de 0 a 1. Se puede enlazar a un slider desde el inspector.</summary>
    public void SetVolume(float value)
    {
        volume = Mathf.Clamp(value, MinVolume, 1f);
        Apply(volume);
        PlayerPrefs.SetFloat(prefsKey, volume);
    }

    private void Apply(float linear)
    {
        if (mixer != null)
        {
            // El mixer trabaja en decibelios (escala logarítmica): 1 -> 0 dB, 0.0001 -> -80 dB
            mixer.SetFloat(exposedParameter, Mathf.Log10(linear) * 20f);
        }
        else
        {
            AudioListener.volume = linear;
        }
    }
}