using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
    public static MusicPlayer Instance { get; private set; }
    AudioSource audioSource;
    const string PREF_KEY = "musicVolume";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = GetComponent<AudioSource>();

            // Aplicar volumen guardado (si existe)
            float saved = PlayerPrefs.GetFloat(PREF_KEY, audioSource.volume > 0 ? audioSource.volume : 1f);
            audioSource.volume = Mathf.Clamp01(saved);

            // Asegurarse que esté sonando si quieres reproducción automática
            if (!audioSource.isPlaying && audioSource.clip != null)
                audioSource.Play();

            Debug.Log($"[MusicPlayer] Awake - volumen aplicado: {audioSource.volume}");
        }
        else
        {
            // Si ya había otra instancia, destruimos esta (evita duplicados)
            Destroy(gameObject);
        }
    }

    public void SetVolume(float value)
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        audioSource.volume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(PREF_KEY, audioSource.volume);
        PlayerPrefs.Save();
        Debug.Log($"[MusicPlayer] SetVolume -> {audioSource.volume}");
    }

    public float GetVolume()
    {
        return audioSource != null ? audioSource.volume : PlayerPrefs.GetFloat(PREF_KEY, 1f);
    }
}

