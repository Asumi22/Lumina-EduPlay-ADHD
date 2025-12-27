using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// AuthUIController (versión con debugging y tolerancia en parsing de año).
/// - inputEmailRegister se usa como username (sin @) para NIÑOS.
/// - En registro se valida nombre, username, password y año seleccionado.
/// - Si algo falla imprime información detallada en consola para depuración.
/// </summary>
public class AuthUIController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject authPanel;        // panel completo (semi-transparente)
    public GameObject loginView;        // child: login
    public GameObject registerView;     // child: register

    [Header("Login")]
    public TMP_InputField inputEmailLogin;    // puede ser email o username
    public TMP_InputField inputPasswordLogin;
    public Button buttonLogin;
    public Button buttonToRegister;

    [Header("Register (NIÑO)")]
    public TMP_InputField inputNameRegister;
    public TMP_InputField inputEmailRegister; // USAR como username (sin @)
    public TMP_InputField inputPasswordRegister;
    public TMP_Text textSelectedYear;         // <-- asegúrate de asignar el mismo TMP en Inspector
    public Button buttonRegister;
    public Button buttonToLogin;

    [Header("General")]
    public Button closeButton;
    public string levelToLoad = "Level1";

    bool processing = false;

    void Start()
    {
        if (buttonLogin != null) buttonLogin.onClick.AddListener(OnLoginClicked);
        if (buttonToRegister != null) buttonToRegister.onClick.AddListener(ShowRegisterView);
        if (buttonRegister != null) buttonRegister.onClick.AddListener(OnRegisterClicked);
        if (buttonToLogin != null) buttonToLogin.onClick.AddListener(ShowLoginView);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);

        ShowLoginView();
    }

    public void OpenAuthPanel()
    {
        if (authPanel == null) { Debug.LogWarning("[AuthUI] authPanel no asignado."); return; }
        authPanel.SetActive(true);
        authPanel.transform.SetAsLastSibling();

        var cg = authPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = authPanel.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        var canvas = authPanel.GetComponent<Canvas>();
        if (canvas != null) { canvas.overrideSorting = true; canvas.sortingOrder = 500; }

        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        ShowLoginView();
    }

    public void ClosePanel()
    {
        if (authPanel != null) authPanel.SetActive(false);
    }

    public void ShowLoginView()
    {
        if (loginView != null) loginView.SetActive(true);
        if (registerView != null) registerView.SetActive(false);
    }

    public void ShowRegisterView()
    {
        if (loginView != null) loginView.SetActive(false);
        if (registerView != null) registerView.SetActive(true);
    }

    // ----------------- REGISTER (NIÑO) -----------------
    private void OnRegisterClicked()
    {
        if (processing) return;
        processing = true;
        if (buttonRegister != null) buttonRegister.interactable = false;

        string nombre = inputNameRegister?.text.Trim() ?? "";
        string username = inputEmailRegister?.text.Trim().ToLower() ?? ""; // usar como username
        string pass = inputPasswordRegister?.text ?? "";

        // DEBUG: imprime lo que recibimos antes de intentar parsear
        string yearText = (textSelectedYear != null) ? textSelectedYear.text : "<textSelectedYear NO asignado>";
        Debug.Log($"[AuthUI DEBUG] Datos registro -> nombre:'{nombre}', username:'{username}', passLength:{(pass==null?0:pass.Length)}, yearText:'{yearText}'");

        // parse año con tolerancia
        int anio = 0;
        if (textSelectedYear != null)
        {
            string cleaned = textSelectedYear.text?.Trim() ?? "";
            // permitir formatos raros: "5 años", "5 ", etc. extraer primer número
            if (!string.IsNullOrEmpty(cleaned))
            {
                // extraer dígitos iniciales
                string digits = "";
                foreach (char c in cleaned)
                {
                    if (char.IsDigit(c)) digits += c;
                    else if (digits.Length > 0) break; // ya hemos extraído un número y encontramos algo distinto -> stop
                }
                if (digits.Length > 0)
                {
                    int.TryParse(digits, out anio);
                }
                else
                {
                    // intentar parse directo (por si el texto usa coma/punto u otros)
                    int.TryParse(cleaned, out anio);
                }
            }
        }
        else
        {
            Debug.LogWarning("[AuthUI] textSelectedYear no está asignado en el Inspector. Usando fallback (6).");
            anio = 6; // fallback razonable
        }

        // DEBUG: después del parse
        Debug.Log($"[AuthUI DEBUG] año parseado -> {anio}");

        // Validaciones
        if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(pass) || anio == 0)
        {
            // Mensajes más explícitos para ayudar a depurar
            if (string.IsNullOrEmpty(nombre)) Debug.LogWarning("[AuthUI] Falta Nombre.");
            if (string.IsNullOrEmpty(username)) Debug.LogWarning("[AuthUI] Falta Username (usa el campo Correo en el formulario como username).");
            if (string.IsNullOrEmpty(pass)) Debug.LogWarning("[AuthUI] Falta Contraseña.");
            if (anio == 0) Debug.LogWarning("[AuthUI] Año inválido o no seleccionado. textSelectedYear.text = '" + ((textSelectedYear!=null)?textSelectedYear.text:"<null>") + "'");

            Debug.LogWarning("[AuthUI] Completa todos los campos para registrarte (nombre, username, contraseña, año).");
            processing = false;
            if (buttonRegister != null) buttonRegister.interactable = true;
            return;
        }

        if (FirebaseInit.Instance == null)
        {
            Debug.LogWarning("[AuthUI] FirebaseInit no encontrado.");
            processing = false;
            if (buttonRegister != null) buttonRegister.interactable = true;
            return;
        }

        // Llamada real a registro (RegisterChild en FirebaseInit)
        FirebaseInit.Instance.RegisterChild(username, pass, nombre, anio, (success, msg) =>
        {
            processing = false;
            if (buttonRegister != null) buttonRegister.interactable = true;

            if (success)
            {
                Debug.Log("[AuthUI] Registro child exitoso -> cargando nivel");
                ClosePanel();
                SceneManager.LoadScene(levelToLoad);
            }
            else
            {
                Debug.LogWarning("[AuthUI] Registro child falló: " + msg);
            }
        });
    }

    // ----------------- LOGIN -----------------
    private void OnLoginClicked()
    {
        if (processing) return;
        processing = true;
        if (buttonLogin != null) buttonLogin.interactable = false;

        string userOrEmail = inputEmailLogin?.text.Trim() ?? "";
        string pass = inputPasswordLogin?.text ?? "";

        if (string.IsNullOrEmpty(userOrEmail) || string.IsNullOrEmpty(pass))
        {
            Debug.LogWarning("[AuthUI] Completa usuario/email y contraseña.");
            processing = false;
            if (buttonLogin != null) buttonLogin.interactable = true;
            return;
        }

        if (FirebaseInit.Instance == null)
        {
            Debug.LogWarning("[AuthUI] FirebaseInit no encontrado.");
            processing = false;
            if (buttonLogin != null) buttonLogin.interactable = true;
            return;
        }

        if (userOrEmail.Contains("@"))
        {
            string email = userOrEmail.ToLower();
            FirebaseInit.Instance.SignInUser(email, pass, (success, msg) =>
            {
                processing = false;
                if (buttonLogin != null) buttonLogin.interactable = true;

                if (success)
                {
                    Debug.Log("[AuthUI] Login OK (email) -> cargando nivel");
                    ClosePanel();
                    SceneManager.LoadScene(levelToLoad);
                }
                else
                {
                    Debug.LogWarning("[AuthUI] Login (email) falló: " + msg);
                }
            });
        }
        else
        {
            string username = userOrEmail.ToLower();
            FirebaseInit.Instance.SignInChild(username, pass, (success, msg) =>
            {
                processing = false;
                if (buttonLogin != null) buttonLogin.interactable = true;

                if (success)
                {
                    Debug.Log("[AuthUI] Login child OK -> cargando nivel");
                    ClosePanel();
                    SceneManager.LoadScene(levelToLoad);
                }
                else
                {
                    Debug.LogWarning("[AuthUI] Login child falló: " + msg);
                }
            });
        }
    }
}


