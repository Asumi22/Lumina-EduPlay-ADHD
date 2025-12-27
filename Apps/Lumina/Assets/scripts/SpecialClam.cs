using UnityEngine;

public class SpecialClam : MonoBehaviour
{
    [Header("Question data (sample)")]
    // Puedes editar estos campos en el Inspector si quieres variar la pregunta por ejemplar
    [TextArea] public string questionText = "2 + 2 = ?";
    public string[] options = new string[] { "3", "4", "5" };
    public int correctIndex = 1;

    [Header("Optional audio (played on pickup)")]
    public AudioClip pickupClip;
    public AudioSource audioSource; // si quieres reproducir un pequeño 'pickup' sound antes de la pregunta

    private bool used = false;

    private void Reset()
    {
        // para que por defecto el CapsuleCollider2D sea trigger y Tag esté establecido (si quieres)
        TagCheck();
    }

    private void TagCheck()
    {
        // nothing mandatory — solo recordatorio
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;

        // detectar al jugador por componente (más robusto que por tag)
        VaquitaPlayer vp = other.GetComponent<VaquitaPlayer>();
        if (vp != null)
        {
            used = true;

            // reproducir pickup sound si asignado (no depende de Time.timeScale)
            if (audioSource != null && pickupClip != null)
                audioSource.PlayOneShot(pickupClip);

            // preparar la simple question y enviarla al QuestionManager
            if (QuestionManager.Instance != null)
            {
                QuestionManager.SimpleQuestion q = QuestionManager.Instance.GetRandomQuestion();

                // Llamamos a ShowQuestion; el panel se abre y el juego se pausa
                QuestionManager.Instance.ShowQuestion(q, (bool wasCorrect) =>
                {
                    // callback al responder (opcional). Aquí no hacemos nada extra,
                    // LevelManager ya registra los contadores.
                });
            }
            else
            {
                Debug.LogWarning("[SpecialClam] No QuestionManager en la escena.");
            }

            // Destruir el objeto para que no se vuelva a tocar (si prefieres destruir después de cerrado, comentarlo)
            Destroy(gameObject);
        }
    }
}
