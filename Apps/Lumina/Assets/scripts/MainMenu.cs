using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject optionsMenu;   // MenuOpciones (oculto por defecto)
    public GameObject mainMenu;      // Panel principal con botones Play/Options/Quit
    public GameObject authPanel;     // AuthPanel (login/register)

    [Header("Level")]
    public string levelToLoad = "Level1";

    // Estas variables SOLO existen en Android/Editor. En WebGL desaparecen.
#if !UNITY_WEBGL
    private Coroutine waitForLoginCoroutine;
    private bool waitingForLogin = false;
#endif

    void Start()
    {
        // Asegurar que la instancia de FirebaseInit exista
        try
        {
            FirebaseInit.EnsureInstance();
            Debug.Log("[MainMenu] FirebaseInit.EnsureInstance() llamado en Start.");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[MainMenu] Error al asegurar FirebaseInit: " + ex.Message);
        }

        if (optionsMenu != null) optionsMenu.SetActive(false);
        if (authPanel != null) authPanel.SetActive(false);
        if (mainMenu != null) mainMenu.SetActive(true);
    }

    // ----- BOTONES -----
    public void PlayGame()
    {
        Debug.Log("[MainMenu] PlayGame pressed.");

        if (MusicPlayer.Instance != null)
        {
            var audio = MusicPlayer.Instance.GetComponent<AudioSource>();
            if (audio != null) { audio.Stop(); audio.Play(); }
        }

        // --- LÓGICA DE LOGIN ---

#if UNITY_WEBGL
            // OPCIÓN A: Si es WEBGL
            Debug.Log("[MainMenu] WebGL detectado: Saltando login de Firebase.");
            SceneManager.LoadScene(levelToLoad);
#else
        // OPCIÓN B: Si es ANDROID / EDITOR
        if (FirebaseInit.Instance == null || FirebaseInit.Instance.Auth == null || FirebaseInit.Instance.Auth.CurrentUser == null)
        {
            Debug.Log("[MainMenu] No hay usuario autenticado. Abriendo panel de login/registro...");

            if (authPanel != null)
            {
                OpenAuthPanelVisuals(authPanel);

                if (!waitingForLogin)
                    waitForLoginCoroutine = StartCoroutine(WaitForLoginThenLoad());
            }
            else
            {
                Debug.LogWarning("[MainMenu] authPanel no asignado en inspector.");
            }
            return;
        }

        // Si ya hay usuario
        Debug.Log("[MainMenu] Usuario autenticado. Cargando " + levelToLoad);
        SceneManager.LoadScene(levelToLoad);
#endif
    }

    public void OpenOptions()
    {
        if (optionsMenu == null) return;
        optionsMenu.SetActive(true);
        if (mainMenu != null) mainMenu.SetActive(false);

        var cg = optionsMenu.GetComponent<UnityEngine.CanvasGroup>();
        if (cg == null) cg = optionsMenu.AddComponent<UnityEngine.CanvasGroup>();
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    public void CloseOptions()
    {
        if (optionsMenu != null) optionsMenu.SetActive(false);
        if (mainMenu != null) mainMenu.SetActive(true);
    }

    public void BackToMain()
    {
        CloseOptions();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ----- PANEL AUTH -----
    public void OpenAuthPanel()
    {
        if (authPanel == null) return;
        OpenAuthPanelVisuals(authPanel);
    }

    private void OpenAuthPanelVisuals(GameObject panel)
    {
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();

        var canvas = panel.GetComponent<Canvas>();
        if (canvas == null) canvas = panel.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;

        if (panel.GetComponent<UnityEngine.UI.CanvasScaler>() == null)
            panel.AddComponent<UnityEngine.UI.CanvasScaler>();

        var cg = panel.GetComponent<UnityEngine.CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<UnityEngine.CanvasGroup>();
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        var rt = panel.GetComponent<RectTransform>();
        if (rt != null && rt.localScale == Vector3.zero) rt.localScale = Vector3.one;
    }

    // Estas funciones solo existen si NO es WebGL
#if !UNITY_WEBGL
    IEnumerator WaitForLoginThenLoad(float timeoutSeconds = 30f)
    {
        waitingForLogin = true;
        float t = 0f;

        while (t < timeoutSeconds)
        {
            if (FirebaseInit.Instance != null && FirebaseInit.Instance.Auth != null && FirebaseInit.Instance.Auth.CurrentUser != null)
            {
                yield return new WaitForSeconds(0.2f);
                Debug.Log("[MainMenu] Inicio de sesión detectado. Cargando " + levelToLoad);
                waitingForLogin = false;
                SceneManager.LoadScene(levelToLoad);
                yield break;
            }

            t += 0.2f;
            yield return new WaitForSeconds(0.2f);
        }

        Debug.LogWarning("[MainMenu] Espera agotada (" + timeoutSeconds + "s).");
        waitingForLogin = false;
    }

    public void CancelWaitForLogin()
    {
        if (waitForLoginCoroutine != null)
        {
            StopCoroutine(waitForLoginCoroutine);
            waitForLoginCoroutine = null;
            waitingForLogin = false;
            Debug.Log("[MainMenu] Espera de login cancelada manualmente.");
        }
    }
#endif
}