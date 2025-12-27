using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class LogoutButton : MonoBehaviour
{
    // Llamar desde OnClick del Button
    public void LogoutAndReturnToMenu()
    {
        if (FirebaseInit.Instance == null)
        {
            Debug.LogWarning("[Logout] FirebaseInit no encontrado. Volviendo al menú.");
            SceneManager.LoadScene("MainMenu");
            return;
        }

        FirebaseInit.Instance.SignOutCurrentUser((ok, msg) =>
        {
            if (ok)
            {
                Debug.Log("[Logout] Cierre de sesión OK: " + msg);
            }
            else
            {
                Debug.LogWarning("[Logout] Cierre de sesión pudo fallar: " + msg);
            }

            // Volver al MainMenu (ajusta el nombre de la escena si no es MainMenu)
            SceneManager.LoadScene("MainMenu");
        });
    }
}
