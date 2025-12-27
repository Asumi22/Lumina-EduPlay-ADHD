package com.example.atentoyactivo.util;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.os.Build;

import androidx.core.app.NotificationCompat;
import androidx.core.app.NotificationManagerCompat;

import com.example.atentoyactivo.R;
import com.google.firebase.database.DataSnapshot;
import com.google.firebase.database.DatabaseReference;
import com.google.firebase.database.FirebaseDatabase;

public class NotificacionReceiver extends BroadcastReceiver {
    @Override
    public void onReceive(Context context, Intent intent) {

        // Permiso Android 13+
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            if (context.checkSelfPermission(android.Manifest.permission.POST_NOTIFICATIONS)
                    != PackageManager.PERMISSION_GRANTED) {
                return;
            }
        }

        // Obtener UID del niño guardado
        String uid = context.getSharedPreferences("app_data", Context.MODE_PRIVATE)
                .getString("child_uid", null);

        if (uid == null) return;  // No hay niño vinculado

        // Cargar Firebase
        FirebaseDatabase db = FirebaseDatabase.getInstance();
        db.getReference("players").child(uid).get().addOnCompleteListener(task -> {
            if (!task.isSuccessful() || !task.getResult().exists()) return;

            DataSnapshot snap = task.getResult();

            long totalCorrect = snap.child("quizStats").child("correctas").getValue(Long.class);
            long totalIncorrect = snap.child("quizStats").child("incorrectas").getValue(Long.class);

            boolean notified10 = false;
            if (snap.child("notifications").child("notificacion_10").exists()) {
                notified10 = Boolean.TRUE.equals(snap.child("notifications").child("notificacion_10").getValue(Boolean.class));
            }

            boolean notified25 = false;
            if (snap.child("notifications").child("notificacion_25").exists()) {
                notified25 = Boolean.TRUE.equals(snap.child("notifications").child("notificacion_25").getValue(Boolean.class));
            }

            boolean notified50 = false;
            if (snap.child("notifications").child("notificacion_50").exists()) {
                notified50 = Boolean.TRUE.equals(snap.child("notifications").child("notificacion_50").getValue(Boolean.class));
            }

            boolean notified100 = false;
            if (snap.child("notifications").child("notificacion_100").exists()) {
                notified100 = Boolean.TRUE.equals(snap.child("notifications").child("notificacion_100").getValue(Boolean.class));
            }

            String mensaje = "¡Su hijo ha alcanzado un total de " + totalCorrect + " éxitos!\n" +
                    "Correctas: " + totalCorrect + " | Incorrectas: " + totalIncorrect;


            DatabaseReference notifRef = db.getReference("players").child(uid).child("notifications");

            if (totalCorrect >= 10 && totalCorrect < 25 && !notified10) {
                enviarNotificacion(context, "Progreso del niño (Bronce)", mensaje);
                notifRef.child("notificacion_10").setValue(true);
            }
            else if (totalCorrect >= 25 && totalCorrect < 50 && !notified25) {
                enviarNotificacion(context, "Progreso del niño (Plata)", mensaje);
                notifRef.child("notificacion_25").setValue(true);
            }
            else if (totalCorrect >= 50 && totalCorrect < 100 && !notified50) {
                enviarNotificacion(context, "Progreso del niño (Oro)", mensaje);
                notifRef.child("notificacion_50").setValue(true);
            }
            else if (totalCorrect >= 100 && !notified100) {
                enviarNotificacion(context, "Progreso del niño (Diamante)", mensaje);
                notifRef.child("notificacion_100").setValue(true);
            }
        });
    }

    private void enviarNotificacion(Context context, String titulo, String mensaje) {
        // Permiso Android 13+
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            if (context.checkSelfPermission(android.Manifest.permission.POST_NOTIFICATIONS)
                    != PackageManager.PERMISSION_GRANTED) {
                return;
            }
        }
        NotificationCompat.Builder builder =
                new NotificationCompat.Builder(context, "default_channel")
                        .setSmallIcon(R.drawable.ic_notification)
                        .setContentTitle(titulo)
                        .setContentText(mensaje)
                        .setStyle(new NotificationCompat.BigTextStyle().bigText(mensaje))
                        .setPriority(NotificationCompat.PRIORITY_HIGH)
                        .setAutoCancel(true);

        NotificationManagerCompat.from(context)
                .notify((int) System.currentTimeMillis(), builder.build());
    }
}

