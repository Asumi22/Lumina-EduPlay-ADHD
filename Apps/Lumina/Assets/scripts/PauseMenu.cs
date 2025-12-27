using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class PauseMenu : MonoBehaviour
{
    [Tooltip("Panel que aparece al pausar (asígnalo en el Inspector)")]
    public GameObject pausePanel;

    [Tooltip("Objeto que contiene los botones móviles (MobileControls) — opcional si lo asignas")]
    public GameObject mobileControls;

    private CanvasGroup pauseCanvasGroup;
    private bool isPaused = false;

    void Start()
    {
        if (pausePanel == null)
        {
            Debug.LogError("[PauseMenu] pausePanel no asignado en el Inspector.");
            enabled = false;
            return;
        }

        // Asegurarnos del CanvasGroup en pausePanel
        pauseCanvasGroup = pausePanel.GetComponent<CanvasGroup>();
        if (pauseCanvasGroup == null)
            pauseCanvasGroup = pausePanel.AddComponent<CanvasGroup>();

        // Inicialmente el panel no debe bloquear ni ser interactuable
        pauseCanvasGroup.interactable = false;
        pauseCanvasGroup.blocksRaycasts = false;
        pauseCanvasGroup.alpha = 1f; // mantener la visibilidad por si está activo

        // Si no asignaste mobileControls en el Inspector, intentar buscar por nombre en la jerarquía
        if (mobileControls == null)
        {
            var found = GameObject.Find("MobileControls");
            if (found != null)
            {
                mobileControls = found;
                Debug.Log("[PauseMenu] mobileControls encontrado por nombre: MobileControls");
            }
            else
            {
                Debug.LogWarning("[PauseMenu] mobileControls no asignado y no se encontró GameObject 'MobileControls' en la escena.");
            }
        }

        // Asegurar estado inicial: si el panel está activo al inicio, sincronizamos
        bool startedPaused = pausePanel.activeSelf;
        isPaused = startedPaused;
        if (mobileControls != null)
            mobileControls.SetActive(!startedPaused);

        // Si el panel estaba activo, dejar su CanvasGroup preparado para recibir raycasts
        pauseCanvasGroup.interactable = startedPaused;
        pauseCanvasGroup.blocksRaycasts = startedPaused;
    }

    void Update()
    {
        // Tecla de retroceso / Escape para Android / PC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        pauseCanvasGroup.interactable = true;
        pauseCanvasGroup.blocksRaycasts = true;

        if (mobileControls != null)
            mobileControls.SetActive(false);

        // Opcional: limpiamos selección actual para evitar foco en UI anterior
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        Debug.Log("[PauseMenu] Juego pausado. MobileControls desactivado.");
    }

    public void Resume()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        pauseCanvasGroup.interactable = false;
        pauseCanvasGroup.blocksRaycasts = false;

        if (mobileControls != null)
            mobileControls.SetActive(true);

        Debug.Log("[PauseMenu] Juego reanudado. MobileControls activado.");
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}

