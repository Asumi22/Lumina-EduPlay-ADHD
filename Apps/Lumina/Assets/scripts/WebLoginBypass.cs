using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class WebLoginBypass : MonoBehaviour
{
    [Header("Arrastra aquí tu botón de Login/Entrar")]
    public Button loginButton;

    [Header("Nombre de la escena del juego")]
    public string gameSceneName = "Level1"; // Asegúrate que se llame así tu nivel

    void Start()
    {
        // Este código SOLO se compila y ejecuta si es WebGL
#if UNITY_WEBGL
        if (loginButton != null)
        {
            // 1. Borra lo que sea que el botón hacía antes (Llamar a Firebase)
            loginButton.onClick.RemoveAllListeners();

            // 2. Le dice que cargue el nivel directamente
            loginButton.onClick.AddListener(SaltarLogin);

            Debug.Log("[WebLoginBypass] Estamos en WebGL. Botón hackeado para entrar directo.");
        }
#endif
    }

    void SaltarLogin()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}