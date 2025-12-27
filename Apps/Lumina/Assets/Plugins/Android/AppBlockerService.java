package com.lumina.parentalcontrol;

import android.accessibilityservice.AccessibilityService;
import android.accessibilityservice.AccessibilityServiceInfo;
import android.view.accessibility.AccessibilityEvent;
import android.content.SharedPreferences;
import android.util.Log;
import android.widget.Toast;
import android.os.Handler;
import android.os.Looper;
import java.util.HashSet;
import java.util.Set;
import android.content.Context;

public class AppBlockerService extends AccessibilityService {
    
    private static final String TAG = "LuminaBlocker";
    private static final String PREFS_NAME = "LuminaPrefs";
    private static final String BLOCKED_KEY = "BlockedPackages";

    public static void UpdateBlockedList(Context context, String[] packages) {
        if (context == null) return;
        try {
            SharedPreferences prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
            SharedPreferences.Editor editor = prefs.edit();
            Set<String> set = new HashSet<>();
            int count = 0;
            if (packages != null) {
                for (String p : packages) {
                    set.add(p);
                    count++;
                }
            }
            editor.putStringSet(BLOCKED_KEY, set);
            editor.commit(); 
            showToast(context, "Lumina: Seguridad actualizada (" + count + " reglas)");
        } catch (Exception e) {
            Log.e(TAG, "Error: " + e.getMessage());
        }
    }

    @Override
    public void onAccessibilityEvent(AccessibilityEvent event) {
        // Detectar cambios de ventana
        if (event.getEventType() == AccessibilityEvent.TYPE_WINDOW_STATE_CHANGED) {
            if (event.getPackageName() != null) {
                String currentPackage = event.getPackageName().toString();
                
                // 1. PROTECCIÓN: Nunca bloquearse a sí mismo
                // Si el paquete es el mismo que este servicio (Lumina), no hacer nada.
                if (currentPackage.equals(getPackageName())) {
                    return;
                }

                // 2. VERIFICACIÓN DE BLOQUEO
                if (isPackageBlocked(currentPackage)) {
                    Log.w(TAG, "BLOQUEANDO: " + currentPackage);
                    
                    showToast(getApplicationContext(), "⛔ App Bloqueada por Lumina");
                    
                    // 3. ACCIÓN AGRESIVA: Pulsar botón HOME del sistema
                    performGlobalAction(GLOBAL_ACTION_HOME);
                } 
                else {
                    // --- MODO DETECTIVE ---
                    // (Esto te mostrará el nombre de la app que abres para verificar si es el correcto)
                    // Cuando todo funcione, puedes borrar o comentar esta línea:
                    // showToast(getApplicationContext(), "Abriendo: " + currentPackage);
                }
            }
        }
    }

    private boolean isPackageBlocked(String packageName) {
        SharedPreferences prefs = getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
        Set<String> blockedSet = prefs.getStringSet(BLOCKED_KEY, new HashSet<String>());
        return blockedSet.contains(packageName);
    }

    private static void showToast(Context context, String msg) {
        new Handler(Looper.getMainLooper()).post(() -> 
            Toast.makeText(context, msg, Toast.LENGTH_SHORT).show()
        );
    }

    @Override
    public void onInterrupt() {}

    @Override
    protected void onServiceConnected() {
        super.onServiceConnected();
        AccessibilityServiceInfo info = new AccessibilityServiceInfo();
        info.eventTypes = AccessibilityEvent.TYPE_WINDOW_STATE_CHANGED;
        info.feedbackType = AccessibilityServiceInfo.FEEDBACK_GENERIC;
        info.flags = AccessibilityServiceInfo.FLAG_INCLUDE_NOT_IMPORTANT_VIEWS;
        info.notificationTimeout = 100;
        setServiceInfo(info);
    }
}