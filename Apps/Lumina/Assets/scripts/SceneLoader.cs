using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    [Header("UI")]
    [Tooltip("Arrastra aquí el PanelCarga (el hijo que tiene la imagen y el texto)")]
    public GameObject loadingScreen;

    void Awake()
    {
        // Configuración del Singleton
        if (Instance == null)
        {
            Instance = this;
            // Opcional: DontDestroyOnLoad(gameObject); // Si quisieras que este manager sobreviva entre escenas
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Asegurarse de que la pantalla de carga empiece apagada al iniciar el nivel
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[SceneLoader] Advertencia: No has asignado el 'Loading Screen' en el Inspector.");
        }
    }

    // ✅ Este es el método que tu Puerta (DoorController) llama
    public void LoadSceneWithLoading(string sceneName)
    {
        Debug.Log("[SceneLoader] 1. Recibida orden de cargar escena: " + sceneName);
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // 1. Activar la pantalla visual
        if (loadingScreen != null)
        {
            Debug.Log("[SceneLoader] 2. Activando pantalla de carga (PanelCarga)...");
            loadingScreen.SetActive(true);
        }
        else
        {
            Debug.LogError("[SceneLoader] ERROR: ¡El campo 'Loading Screen' está vacío en el Inspector!");
        }

        // 2. Iniciar la carga asíncrona
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        // Evitar que cambie de escena inmediatamente
        operation.allowSceneActivation = false;

        float timer = 0f;

        // 3. Esperar mientras carga y animar
        while (!operation.isDone)
        {
            timer += Time.deltaTime;

            // La carga termina cuando progress llega a 0.9
            // Añadimos la condición timer >= 2f para forzar que se vea la animación al menos 2 segundos
            if (operation.progress >= 0.9f && timer >= 2f)
            {
                Debug.Log("[SceneLoader] 3. Carga terminada y tiempo cumplido. Cambiando nivel.");
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}