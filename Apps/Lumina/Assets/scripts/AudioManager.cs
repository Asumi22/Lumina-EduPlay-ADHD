using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Componentes AudioSource")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Música y SFX Fijos")]
    [SerializeField] public AudioClip backgroundMusic;
    [SerializeField] public AudioClip coinSound;
    [SerializeField] public AudioClip barrelBreakSound;
    [SerializeField] public AudioClip playerDeathSound;
    [SerializeField] public AudioClip confirmationSound;

    [Header("Voces (Resguardo / Default)")]
    [SerializeField] public AudioClip welcomeVoice;
    [SerializeField] public AudioClip[] successVoices;
    [SerializeField] public AudioClip[] damageVoices;
    [SerializeField] public AudioClip fallDeathVoice;
    [SerializeField] public AudioClip livesDeathVoice;

    // Claves de volumen
    private const string PREF_MUSIC_KEY = "musicVolume";
    private const string PREF_SFX_KEY = "sfxVolume";

    // Configuración de Aleatoriedad
    private const int DAMAGE_VARIANTS = 3;  // damage_0, damage_1, damage_2
    private const int SUCCESS_VARIANTS = 2; // success_0, success_1

    private Dictionary<string, AudioClip> audioCache;

    // Nombres exactos de tus carpetas (según tu imagen)
    private const string FOLDER_DAMAGE = "Voces de Daño (Damage)";
    private const string FOLDER_SUCCESS = "Voces de Éxito (Success)";
    private const string FOLDER_WELCOME = "Voz de Bienvenida (Welcome)";
    private const string FOLDER_DEATH = "Voz de Muerte (Death)";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            audioCache = new Dictionary<string, AudioClip>();

            if (musicSource == null || sfxSource == null)
            {
                AudioSource[] sources = GetComponents<AudioSource>();
                if (sources.Length >= 1) musicSource = sources[0];
                else musicSource = gameObject.AddComponent<AudioSource>();

                if (sources.Length >= 2) sfxSource = sources[1];
                else sfxSource = gameObject.AddComponent<AudioSource>();
            }

            // Cargar volúmenes
            float musicVol = PlayerPrefs.GetFloat(PREF_MUSIC_KEY, 1.0f);
            float sfxVol = PlayerPrefs.GetFloat(PREF_SFX_KEY, 1.0f);

            musicSource.volume = musicVol;
            sfxSource.volume = sfxVol;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (backgroundMusic != null && musicSource != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    // --- VOLUMEN ---
    public void SetMusicVolume(float value)
    {
        float v = Mathf.Clamp01(value);
        if (musicSource != null) musicSource.volume = v;
        PlayerPrefs.SetFloat(PREF_MUSIC_KEY, v);
    }

    public void SetSfxVolume(float value)
    {
        float v = Mathf.Clamp01(value);
        if (sfxSource != null) sfxSource.volume = v;
        PlayerPrefs.SetFloat(PREF_SFX_KEY, v);
    }

    // --- SFX FIJOS ---
    public void PlayCoinSound() { PlaySoundClip(coinSound); }
    public void PlayBarrelBreak() { PlaySoundClip(barrelBreakSound); }
    public void PlayConfirmationSound() { PlaySoundClip(confirmationSound); }

    // --- VOCES (Adaptadas a tus carpetas) ---

    public void PlayWelcomeVoice()
    {
        // Busca en: Sounds/Voices/{LANG}/Voz de Bienvenida (Welcome)/welcome
        if (!PlayDynamicClip("welcome", FOLDER_WELCOME))
            PlayVoiceClip(welcomeVoice);
    }

    public void PlayPlayerDeath()
    {
        // Busca en: Sounds/Voices/{LANG}/Voz de Muerte (Death)/death
        if (!PlayDynamicClip("death", FOLDER_DEATH))
            PlayVoiceClip(playerDeathSound != null ? playerDeathSound : barrelBreakSound);
    }

    public void PlayFallDeathVoice()
    {
        if (!PlayDynamicClip("death", FOLDER_DEATH)) PlayVoiceClip(fallDeathVoice);
    }

    public void PlayLivesDeathVoice()
    {
        if (!PlayDynamicClip("death", FOLDER_DEATH)) PlayVoiceClip(livesDeathVoice);
    }

    public void PlaySuccessVoice()
    {
        // Busca en: Sounds/Voices/{LANG}/Voces de Éxito (Success)/success_X
        int rnd = Random.Range(0, SUCCESS_VARIANTS);
        if (!PlayDynamicClip("success_" + rnd, FOLDER_SUCCESS))
        {
            if (successVoices != null && successVoices.Length > 0)
                PlayVoiceClip(successVoices[Random.Range(0, successVoices.Length)]);
        }
    }

    public void PlayDamageVoice()
    {
        // Busca en: Sounds/Voices/{LANG}/Voces de Daño (Damage)/damage_X
        int rnd = Random.Range(0, DAMAGE_VARIANTS);
        if (!PlayDynamicClip("damage_" + rnd, FOLDER_DAMAGE))
        {
            if (damageVoices != null && damageVoices.Length > 0)
                PlayVoiceClip(damageVoices[Random.Range(0, damageVoices.Length)]);
        }
    }

    public void PlayQuestionVoice(int questionID)
    {
        // Las preguntas suelen estar en la raíz del idioma o en su propia carpeta.
        // Si las tienes sueltas en "EN", usa cadena vacía "".
        // Si las metiste en una carpeta "Preguntas", cambia el segundo parámetro.
        PlayDynamicClip("q_" + questionID, "");
    }

    public void PlayAnswerVoice(int questionID, int answerIndex)
    {
        PlayDynamicClip("a_" + questionID + "_" + answerIndex, "");
    }


    // --- LÓGICA INTERNA CORREGIDA ---

    // Ahora acepta "subfolder" para navegar tu estructura
    private bool PlayDynamicClip(string clipName, string subfolder)
    {
        string lang = "ES";
        if (LanguageManager.Instance != null) lang = LanguageManager.Instance.currentLanguage;

        // Construir ruta: Sounds/Voices/EN/NombreCarpeta/archivo
        string basePath = "Sounds/Voices/" + lang + "/";
        if (!string.IsNullOrEmpty(subfolder)) basePath += subfolder + "/";

        string fullPath = basePath + clipName;

        AudioClip clip = null;

        if (audioCache.ContainsKey(fullPath)) clip = audioCache[fullPath];
        else
        {
            clip = Resources.Load<AudioClip>(fullPath);
            if (clip != null) audioCache.Add(fullPath, clip);
        }

        // Fallback a español si falla (intentando la misma estructura en ES)
        if (clip == null && lang != "ES")
        {
            string fallbackBase = "Sounds/Voices/ES/";
            if (!string.IsNullOrEmpty(subfolder)) fallbackBase += subfolder + "/";
            string fallbackPath = fallbackBase + clipName;

            clip = Resources.Load<AudioClip>(fallbackPath);
        }

        if (clip != null)
        {
            PlayVoiceClip(clip);
            return true;
        }

        // Debug para ayudarte si falla
        // Debug.LogWarning("No se encontró audio en: " + fullPath);
        return false;
    }

    private void PlaySoundClip(AudioClip clip)
    {
        if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip, sfxSource.volume);
    }

    private void PlayVoiceClip(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.Stop();
            sfxSource.PlayOneShot(clip, sfxSource.volume);
        }
    }

    public void ChangeMusic(AudioClip newMusic)
    {
        if (musicSource == null) return;
        backgroundMusic = newMusic;
        musicSource.Stop();
        musicSource.clip = backgroundMusic;
        musicSource.loop = true;
        musicSource.Play();
    }
}