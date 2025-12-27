using UnityEngine;

public class VidaJugador : MonoBehaviour
{
    [Header("Configuración de vida")]
    public int vidaMaxima = 3;
    [HideInInspector] public int vidaActual;

    // (Opcional) campos de fallback si no tienes LevelManager en escena. No es necesario si usas LevelManager.
    public GameObject Corazon1;
    public GameObject Corazon2;
    public GameObject Corazon3;
    public GameObject Corazon4;
    public GameObject Corazon5;
    public GameObject Corazon6;

    void Start()
    {
        vidaActual = vidaMaxima;
    }

    // Este método se llamaba desde QuestionManager. Ahora delega a LevelManager si existe.
    public void QuitarVida(int cantidad)
    {
        if (cantidad <= 0) return;

        if (LevelManager.Instance != null)
        {
            // Pedimos a LevelManager que reste la vida (sin knockback)
            bool died = LevelManager.Instance.LoseLifeFromQuestion(cantidad);
            vidaActual = LevelManager.Instance.CurrentLives;
            if (died)
            {
                // LevelManager ya llamó a ForceDie / reinicio.
            }
            return;
        }

        // --- Fallback local si LevelManager no está presente ---
        vidaActual -= cantidad;
        if (vidaActual < 0) vidaActual = 0;
        ActualizarCorazonesFallback();

        if (vidaActual <= 0)
        {
            Debug.Log("Jugador murió (fallback VidaJugador).");
            // reiniciar escena si deseas
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }

    private void ActualizarCorazonesFallback()
    {
        // Asume que tienes 3 corazones llenos (1-3) y 3 vacíos (4-6). Si no los asignaste, este método no hace nada.
        if (Corazon1 == null || Corazon2 == null || Corazon3 == null || Corazon4 == null || Corazon5 == null || Corazon6 == null)
            return;

        if (vidaActual == 3)
        {
            Corazon1.SetActive(true);
            Corazon2.SetActive(true);
            Corazon3.SetActive(true);
            Corazon4.SetActive(false);
            Corazon5.SetActive(false);
            Corazon6.SetActive(false);
        }
        else if (vidaActual == 2)
        {
            Corazon1.SetActive(true);
            Corazon2.SetActive(true);
            Corazon3.SetActive(false);
            Corazon4.SetActive(false);
            Corazon5.SetActive(false);
            Corazon6.SetActive(true);
        }
        else if (vidaActual == 1)
        {
            Corazon1.SetActive(true);
            Corazon2.SetActive(false);
            Corazon3.SetActive(false);
            Corazon4.SetActive(false);
            Corazon5.SetActive(true);
            Corazon6.SetActive(true);
        }
        else
        {
            Corazon1.SetActive(false);
            Corazon2.SetActive(false);
            Corazon3.SetActive(false);
            Corazon4.SetActive(true);
            Corazon5.SetActive(true);
            Corazon6.SetActive(true);
        }
    }
}
