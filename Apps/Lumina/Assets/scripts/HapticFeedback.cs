using UnityEngine;

public class HapticFeedback : MonoBehaviour
{
#pragma warning disable CS0414
    AndroidJavaObject vibrator = null;
    AndroidJavaObject currentActivity = null;
    int sdkInt = 0;
#pragma warning restore CS0414

    void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            var versionClass = new AndroidJavaClass("android.os.Build$VERSION");
            sdkInt = versionClass.GetStatic<int>("SDK_INT");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[HapticFeedback] No se pudo inicializar Vibrator nativo: " + e.Message);
            vibrator = null;
            currentActivity = null;
        }
#endif
    }

    /// <summary>
    /// Vibra durante ms milisegundos. Ej: Vibrate(50) = vibra 0.05s.
    /// </summary>
    public void Vibrate(int ms = 10)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (vibrator == null)
            {
                Handheld.Vibrate();
                return;
            }

            if (sdkInt >= 26)
            {
                AndroidJavaClass veClass = new AndroidJavaClass("android.os.VibrationEffect");
                AndroidJavaObject effect = veClass.CallStatic<AndroidJavaObject>("createOneShot", ms, -1);
                vibrator.Call("vibrate", effect);
            }
            else
            {
                vibrator.Call("vibrate", ms);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[HapticFeedback] Excepción vibrando: " + e.Message + " -> fallback Handheld.Vibrate()");
            Handheld.Vibrate();
        }
#elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();
#else
        // En PC/Editor no vibra (pero no lanza warnings)
#endif
    }

    // Helpers para usar directo en el inspector
    public void Vibrate50() => Vibrate(50);   // 0.05s
    public void Vibrate100() => Vibrate(100); // 0.1s
}
