package com.example.atentoyactivo;

import android.annotation.TargetApi;
import android.app.AlarmManager;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;

import androidx.activity.EdgeToEdge;
import androidx.appcompat.app.AppCompatActivity;

import com.example.atentoyactivo.util.NotificacionReceiver;

public class MainActivity extends AppCompatActivity {
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        EdgeToEdge.enable(this);
        setContentView(R.layout.activity_main);
        //Pide permisos de notificación
        solicitarPermisoNotificaciones();
        createNotificationChannel();
        //Verifica permisos de alarma
        if (!tienePermisoExactAlarm()) {
            pedirPermisoExactAlarm();
        }
        // Verificar a dónde debe ir después del splash
        String nextScreen = getIntent().getStringExtra("nextScreen");

        new Handler(Looper.getMainLooper()).postDelayed(() -> {
            if ("dashboard".equals(nextScreen)) {
                startActivity(new Intent(MainActivity.this, DashboardActivity.class));
            } else {
                startActivity(new Intent(MainActivity.this, LoginActivity.class));
            }
            programarNotificacionPeriodica(1, "Progreso de tu niño", "Revisando datos...");
            finish();
        }, 5000);
    }
    private void createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            String channelId = "default_channel";
            String channelName = "Notificaciones Generales";
            String channelDesc = "Canal para notificaciones comunes";

            NotificationChannel channel = new NotificationChannel(
                    channelId,
                    channelName,
                    NotificationManager.IMPORTANCE_HIGH
            );
            channel.setDescription(channelDesc);

            NotificationManager manager = getSystemService(NotificationManager.class);
            manager.createNotificationChannel(channel);
        }
    }
    private static final int REQUEST_NOTIFICATION_PERMISSION = 1;

    private void solicitarPermisoNotificaciones() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            if (checkSelfPermission(android.Manifest.permission.POST_NOTIFICATIONS)
                    != PackageManager.PERMISSION_GRANTED) {

                requestPermissions(
                        new String[]{android.Manifest.permission.POST_NOTIFICATIONS},
                        REQUEST_NOTIFICATION_PERMISSION
                );
            }
        }
    }
    public void programarNotificacionPeriodica(int minutos, String titulo, String mensaje) {

        long intervalo = minutos * 60 * 1000;

        Intent intent = new Intent(this, NotificacionReceiver.class);
        intent.putExtra("titulo", titulo);
        intent.putExtra("mensaje", mensaje);

        PendingIntent pendingIntent = PendingIntent.getBroadcast(
                this,
                1002,
                intent,
                PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE
        );

        AlarmManager alarmManager = (AlarmManager) getSystemService(ALARM_SERVICE);

        alarmManager.setInexactRepeating(
                AlarmManager.RTC_WAKEUP,
                System.currentTimeMillis() + intervalo,
                intervalo,
                pendingIntent
        );
    }
    private boolean tienePermisoExactAlarm() {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.S) {
            return true; // Antes de Android 12 no existe el permiso
        }

        AlarmManager alarmManager = (AlarmManager) getSystemService(ALARM_SERVICE);
        return alarmManager.canScheduleExactAlarms();
    }
    @TargetApi(Build.VERSION_CODES.S)
    private void pedirPermisoExactAlarm() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            AlarmManager alarmManager = (AlarmManager) getSystemService(ALARM_SERVICE);

            if (!alarmManager.canScheduleExactAlarms()) {
                Intent intent = new Intent(android.provider.Settings.ACTION_REQUEST_SCHEDULE_EXACT_ALARM);
                intent.setData(Uri.parse("package:" + getPackageName()));
                startActivity(intent);
            }
        }
    }
}

