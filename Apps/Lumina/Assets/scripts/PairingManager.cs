using UnityEngine;
using TMPro;
using System.Collections;

// Solo importa Firebase si NO es WebGL
#if !UNITY_WEBGL
using Firebase.Database;
using Firebase.Extensions;
#endif

public class PairingManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject pairingPanel;
    public TMP_Text codeText;
    public GameObject loadingText;

#if !UNITY_WEBGL
    private DatabaseReference dbRef;
    // Esta variable solo se usa en Android, así que la escondemos en WebGL para evitar el aviso amarillo
    private string currentCode = "";
#endif

    void Start()
    {
        if (pairingPanel != null) pairingPanel.SetActive(false);
        if (loadingText != null) loadingText.SetActive(false);

#if !UNITY_WEBGL
        StartCoroutine(WaitForFirebase());
#endif
    }

#if !UNITY_WEBGL
    private IEnumerator WaitForFirebase()
    {
        yield return new WaitForSeconds(0.5f);
        if (FirebaseInit.Instance != null)
        {
            dbRef = FirebaseInit.Instance.DbReference;
        }
    }
#endif

    public void OpenPairingPanel()
    {
        // En WebGL, mostramos un mensaje dummy
#if UNITY_WEBGL
        if(pairingPanel != null) pairingPanel.SetActive(true);
        if(codeText != null) codeText.text = "WEB-DEMO";
        return;
#endif

#if !UNITY_WEBGL
        if (FirebaseInit.Instance == null || FirebaseInit.Instance.CurrentUser == null)
        {
            Debug.LogWarning("[Pairing] No hay usuario logueado.");
            return;
        }

        pairingPanel.SetActive(true);
        if (loadingText != null) loadingText.SetActive(true);
        codeText.text = "";

        GenerateAndUploadCode();
#endif
    }

#if !UNITY_WEBGL
    private void GenerateAndUploadCode()
    {
        currentCode = GenerateRandomString(6);
        string displayCode = currentCode.Insert(3, "-");
        string uid = FirebaseInit.Instance.CurrentUser.UserId;

        if (dbRef == null) dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        dbRef.Child("pairing_codes").Child(currentCode).SetValueAsync(uid)
            .ContinueWithOnMainThread(task =>
            {
                if (loadingText != null) loadingText.SetActive(false);

                if (task.IsFaulted)
                {
                    codeText.text = "Error";
                    Debug.LogError("[Pairing] Error subiendo código: " + task.Exception);
                }
                else
                {
                    codeText.text = displayCode;
                    Debug.Log($"[Pairing] Código {currentCode} subido para UID {uid}");
                    StartCoroutine(ExpireCodeRoutine(currentCode));
                }
            });
    }

    public void ClosePanel()
    {
        if (!string.IsNullOrEmpty(currentCode) && dbRef != null)
        {
            dbRef.Child("pairing_codes").Child(currentCode).RemoveValueAsync();
        }
        pairingPanel.SetActive(false);
    }

    private string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        char[] stringChars = new char[length];
        for (int i = 0; i < length; i++)
        {
            stringChars[i] = chars[Random.Range(0, chars.Length)];
        }
        return new string(stringChars);
    }

    private IEnumerator ExpireCodeRoutine(string code)
    {
        yield return new WaitForSeconds(300);
        if (dbRef != null)
        {
            dbRef.Child("pairing_codes").Child(code).RemoveValueAsync();
        }
    }
#else
    // Versión vacía para WebGL para que el botón de cerrar funcione sin errores
    public void ClosePanel() 
    {
        if(pairingPanel != null) pairingPanel.SetActive(false);
    }
#endif
}