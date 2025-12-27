using UnityEngine;
using System.Collections.Generic;

// Importar Firebase solo si NO es WebGL
#if !UNITY_WEBGL
using Firebase.Database;
#endif

public class AppBlockerManager : MonoBehaviour
{
    public static AppBlockerManager Instance;

    // Variable de referencia solo para Android/Editor
#if !UNITY_WEBGL
    private DatabaseReference controlsRef;
#endif

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // RunInBackground no es necesario/soportado igual en WebGL
#if !UNITY_WEBGL
            Application.runInBackground = true;
#endif
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartListening(string uid)
    {
        // En WebGL no hacemos nada
#if UNITY_WEBGL
        return;
#endif

        // Código original para Android
#if !UNITY_WEBGL
        if (string.IsNullOrEmpty(uid)) return;

        // Aseguramos conexión
        FirebaseDatabase.DefaultInstance.GoOnline();

        controlsRef = FirebaseDatabase.DefaultInstance.GetReference("usuarios").Child(uid).Child("controles");
        controlsRef.ValueChanged += HandleControlChange;
#endif
    }

    // Estos métodos usan tipos de Firebase (DataSnapshot, ValueChangedEventArgs)
    // Por tanto, deben estar completamente envueltos para no romper WebGL
#if !UNITY_WEBGL
    private void HandleControlChange(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null || !args.Snapshot.Exists) return;

        List<string> blockedList = new List<string>();

        // -- TikTok --
        if (IsAppBlocked(args.Snapshot, "block_tiktok"))
        {
            blockedList.Add("com.zhiliaoapp.musically"); // Global
            blockedList.Add("com.ss.android.ugc.trill"); // Asia/Otros
            blockedList.Add("com.zhiliaoapp.musically.go"); // Lite
            blockedList.Add("com.tiktok.android"); // Otra variante
        }

        // -- YouTube --
        if (IsAppBlocked(args.Snapshot, "block_youtube"))
        {
            blockedList.Add("com.google.android.youtube");
        }

        // -- Instagram --
        if (IsAppBlocked(args.Snapshot, "block_instagram"))
        {
            blockedList.Add("com.instagram.android");
        }

        SendToJava(blockedList.ToArray());
    }

    private bool IsAppBlocked(DataSnapshot snap, string key)
    {
        // Nota: Quitamos los paréntesis de .Exists para evitar errores de versión
        if (snap.Child(key).Exists)
        {
            return bool.Parse(snap.Child(key).Value.ToString());
        }
        return false;
    }
#endif

    private void SendToJava(string[] packages)
    {
        // Este código usa AndroidJavaClass, que NO existe en la compilación WebGL.
        // Debemos protegerlo para que el compilador no falle.
#if UNITY_ANDROID && !UNITY_WEBGL
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject context = currentActivity.Call<AndroidJavaObject>("getApplicationContext"))
            using (AndroidJavaClass serviceClass = new AndroidJavaClass("com.lumina.parentalcontrol.AppBlockerService"))
            {
                // Enviamos el Contexto para poder guardar en memoria persistente
                serviceClass.CallStatic("UpdateBlockedList", context, packages);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[AppBlocker] Error enviando a Java: " + e.Message);
        }
#endif
    }

    public void RequestPermission()
    {
        // Igual aquí, AndroidJavaObject no existe en WebGL
#if UNITY_ANDROID && !UNITY_WEBGL
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", "android.settings.ACCESSIBILITY_SETTINGS"))
            {
                currentActivity.Call("startActivity", intent);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[AppBlocker] Error pidiendo permiso: " + e.Message);
        }
#else
        Debug.Log("[AppBlocker] RequestPermission: No soportado en esta plataforma (WebGL/Editor)");
#endif
    }
}