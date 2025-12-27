using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla DOS sliders (Música y SFX) y los comunica al AudioManager.
/// </summary>
public class VolumeController : MonoBehaviour
{
    [Header("UI Sliders")]
    [Tooltip("Arrastra aquí el slider de MÚSICA de fondo.")]
    public Slider musicSlider;
    [Tooltip("Arrastra aquí el slider de EFECTOS (SFX).")]
    public Slider sfxSlider;

    // Claves para guardar las preferencias del jugador
    const string PREF_MUSIC_KEY = "musicVolume";
    const string PREF_SFX_KEY = "sfxVolume";

    void Start()
    {
        // --- Configurar Slider de Música ---
        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            // Cargar el valor guardado (o 1.0f si es la primera vez)
            float savedMusicVol = PlayerPrefs.GetFloat(PREF_MUSIC_KEY, 1.0f);
            musicSlider.value = savedMusicVol;

            // Informar al AudioManager del valor cargado
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicVolume(savedMusicVol);
            }

            // Añadir el listener para cuando el usuario mueva el slider
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        }
        else
        {
            Debug.LogError("[VolumeController] ¡No se asignó el 'Music Slider'!");
        }

        // --- Configurar Slider de SFX ---
        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            float savedSfxVol = PlayerPrefs.GetFloat(PREF_SFX_KEY, 1.0f);
            sfxSlider.value = savedSfxVol;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSfxVolume(savedSfxVol);
            }

            sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        }
        else
        {
            Debug.LogError("[VolumeController] ¡No se asignó el 'SFX Slider'!");
        }
    }

    void OnDisable()
    {
        // Limpiar listeners
        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
    }

    /// <summary>
    /// Se llama cuando el slider de MÚSICA cambia.
    /// </summary>
    private void OnMusicSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }
        PlayerPrefs.SetFloat(PREF_MUSIC_KEY, value);
    }

    /// <summary>
    /// Se llama cuando el slider de SFX cambia.
    /// </summary>
    private void OnSfxSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSfxVolume(value);
        }
        PlayerPrefs.SetFloat(PREF_SFX_KEY, value);
    }
}