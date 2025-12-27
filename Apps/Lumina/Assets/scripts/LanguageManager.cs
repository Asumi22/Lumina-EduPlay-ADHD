using UnityEngine;

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance;
    public string currentLanguage = "ES"; // ES, EN, QU

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            currentLanguage = PlayerPrefs.GetString("idioma_seleccionado", "ES");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void CambiarIdioma(string nuevoIdioma)
    {
        currentLanguage = nuevoIdioma;
        PlayerPrefs.SetString("idioma_seleccionado", nuevoIdioma);
        PlayerPrefs.Save();
        Debug.Log("Idioma cambiado a: " + currentLanguage);

        // Opcional: Reiniciar la escena para ver los cambios al instante
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}