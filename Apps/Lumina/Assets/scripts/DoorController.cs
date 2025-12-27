using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorController : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Nombre exacto de la escena a cargar (ej: Level2)")]
    public string levelToLoad = "Level2";

    [Tooltip("Tiempo de espera para ver la animación antes de cambiar de nivel")]
    public float loadDelay = 1.5f;

    [Header("Estado Inicial")]
    [Tooltip("Si es true, la puerta empieza cerrada y necesita que el Boss la abra.")]
    public bool startLocked = true;

    private bool isOpen = false;
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();

        // Configurar estado inicial
        if (startLocked)
        {
            isOpen = false;
        }
        else
        {
            isOpen = true; // Si es una puerta normal, empieza abierta
        }
    }

    // --- ESTA FUNCIÓN LA LLAMA EL BOSSFINAL AL MORIR ---
    public void UnlockAndOpen()
    {
        if (isOpen) return; // Si ya está abierta, no hacer nada

        isOpen = true;
        Debug.Log("[DoorController] ¡Puerta desbloqueada!");

        // 1. Sonido
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayConfirmationSound();
        }

        // 2. Animación (Luz)
        if (animator != null)
        {
            animator.SetTrigger("Open");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Si está cerrada, ignoramos al jugador
        if (!isOpen) return;

        // Detectar al jugador
        if (other.CompareTag("Player") || other.GetComponent<VaquitaPlayer>() != null)
        {
            Debug.Log("[DoorController] Jugador entró. Iniciando carga de nivel...");
            StartCoroutine(LoadLevelRoutine());
        }
    }

    private IEnumerator LoadLevelRoutine()
    {
        // Esperar a que termine la animación o delay configurado
        yield return new WaitForSeconds(loadDelay);

        // --- USAR TU SCENELOADER ---
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadSceneWithLoading(levelToLoad);
        }
        else
        {
            // Fallback de seguridad por si olvidaste poner el SceneLoader
            Debug.LogWarning("[DoorController] SceneLoader no encontrado, cargando directo con SceneManager.");
            SceneManager.LoadScene(levelToLoad);
        }
    }
}