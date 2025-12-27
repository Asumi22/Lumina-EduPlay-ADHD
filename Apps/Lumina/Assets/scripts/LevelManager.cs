using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Vidas")]
    public int maxLives = 3;
    private int currentLives;

    [Header("Hearts (use GameObjects)")]
    public GameObject[] fullHearts;   // Heart1, Heart2, Heart3
    public GameObject[] emptyHearts;  // Heart4, Heart5, Heart6

    [Header("Audio (Level)")]
    public AudioSource audioSource;
    public AudioClip damageClip;
    public AudioClip spikeClip;

    [Header("Invulnerabilidad")]
    public float invulnerableDuration = 1.2f;
    public float blinkInterval = 0.12f;
    private bool isInvulnerable = false;

    [Header("Preguntas - estadísticas")]
    public int correctAnswers = 0;
    public int wrongAnswers = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        currentLives = maxLives;
    }

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                Debug.LogWarning("[LevelManager] No AudioSource asignado en LevelManager.");
        }

        // Asegurar empty hearts inactivas
        if (emptyHearts != null)
        {
            for (int i = 0; i < emptyHearts.Length; i++)
            {
                if (emptyHearts[i] != null) emptyHearts[i].SetActive(false);
            }
        }

        UpdateHeartsUI();
    }

    // --- Método usado cuando el jugador recibe daño por enemigo (con knockback e invulnerabilidad) ---
    public void ApplyDamage(GameObject player, Vector2 sourcePosition, float knockback = 5f)
    {
        if (isInvulnerable) return;

        if (damageClip != null && audioSource != null)
            audioSource.PlayOneShot(damageClip);

        // knockback
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 knockbackDir = ((Vector2)player.transform.position - sourcePosition).normalized;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(knockbackDir * knockback, ForceMode2D.Impulse);
        }

        currentLives = Mathf.Max(0, currentLives - 1);
        UpdateHeartsUI();

        // trigger Hurt animation if exists
        Animator anim = player.GetComponent<Animator>();
        if (anim != null)
        {
            anim.ResetTrigger("Hurt");
            anim.SetTrigger("Hurt");
        }

        if (currentLives <= 0)
        {
            VaquitaPlayer vp = player.GetComponent<VaquitaPlayer>();
            if (vp != null) vp.ForceDie();
            else StartCoroutine(RestartScene());
        }
        else
        {
            StartCoroutine(InvulnerabilityCoroutine(player));
        }
    }

    // ---- Nuevo: método público para que preguntas (u otros sistemas) quiten vida sin knockback ----
    // Devuelve true si el jugador quedó sin vidas (murió).
    public bool LoseLifeFromQuestion(int amount)
    {
        if (amount <= 0) return false;

        currentLives = Mathf.Max(0, currentLives - amount);
        UpdateHeartsUI();

        if (currentLives <= 0)
        {
            // buscamos al jugador para ejecutar ForceDie (si existe)
            VaquitaPlayer vp = null;
    #if UNITY_2023_2_OR_NEWER
            vp = FindFirstObjectByType<VaquitaPlayer>();
    #else
            vp = FindObjectOfType<VaquitaPlayer>();
    #endif
            if (vp != null)
            {
                vp.ForceDie();
            }
            else
            {
                StartCoroutine(RestartScene());
            }
            return true;
        }

        return false;
    }

    // Muerte por spikes (inmediata)
    public void ApplySpikeDeath(GameObject player)
    {
        if (spikeClip != null && audioSource != null)
            audioSource.PlayOneShot(spikeClip);

        VaquitaPlayer vp = player.GetComponent<VaquitaPlayer>();
        if (vp != null) vp.ForceDie();
        else StartCoroutine(RestartScene());
    }

    private void UpdateHeartsUI()
    {
        if (fullHearts != null && emptyHearts != null &&
            fullHearts.Length >= maxLives && emptyHearts.Length >= maxLives)
        {
            for (int i = 0; i < maxLives; i++)
            {
                if (fullHearts[i] != null)
                    fullHearts[i].SetActive(i < currentLives);

                if (emptyHearts[i] != null)
                    emptyHearts[i].SetActive(i >= currentLives);
            }
            return;
        }

        Debug.LogWarning("[LevelManager] fullHearts/emptyHearts no configurados correctamente.");
    }

    private IEnumerator InvulnerabilityCoroutine(GameObject player)
    {
        isInvulnerable = true;
        SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
        float elapsed = 0f;

        while (elapsed < invulnerableDuration)
        {
            if (sr != null)
            {
                sr.enabled = false;
                yield return new WaitForSeconds(blinkInterval);
                sr.enabled = true;
                yield return new WaitForSeconds(blinkInterval);
            }
            else
            {
                yield return new WaitForSeconds(blinkInterval * 2f);
            }
            elapsed += blinkInterval * 2f;
        }

        isInvulnerable = false;
    }

    private IEnumerator RestartScene()
    {
        yield return new WaitForSeconds(1.2f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Propiedad pública para leer vidas actuales desde otros scripts
    public int CurrentLives => currentLives;

    // Métodos de registro de preguntas
    public void RegisterCorrectAnswer()
    {
        correctAnswers++;
        Debug.Log($"[LevelManager] CorrectAnswers = {correctAnswers}");
    }

    public void RegisterWrongAnswer()
    {
        wrongAnswers++;
        Debug.Log($"[LevelManager] WrongAnswers = {wrongAnswers}");
    }
}



