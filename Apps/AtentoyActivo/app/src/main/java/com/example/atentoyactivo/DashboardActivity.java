package com.example.atentoyactivo;

import android.Manifest;
import android.app.AlertDialog;
import android.content.pm.PackageManager;
import android.os.Build;
import android.os.Bundle;
import android.text.InputType;
import android.util.Log;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.TextView;
import android.widget.Toast;

import androidx.activity.EdgeToEdge;
import androidx.annotation.NonNull;
import androidx.appcompat.app.AppCompatActivity;
import androidx.core.app.ActivityCompat;
import androidx.core.content.ContextCompat;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import com.example.atentoyactivo.ui.UsuarioAdapter;
import com.google.android.material.card.MaterialCardView;
import com.google.android.material.switchmaterial.SwitchMaterial;
import com.google.firebase.FirebaseApp;
import com.google.firebase.auth.FirebaseAuth;
import com.google.firebase.database.DataSnapshot;
import com.google.firebase.database.DatabaseError;
import com.google.firebase.database.DatabaseReference;
import com.google.firebase.database.FirebaseDatabase;
import com.google.firebase.database.ValueEventListener;

import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.util.Locale;

public class DashboardActivity extends AppCompatActivity {
    private static final String TAG = "DashboardActivity";

    // Interfaz para REST Auth
    interface RestVerifyCallback {
        void onResult(boolean ok, String resultOrUid);
    }

    // Vistas
    TextView tvTotalCorrectCount, tvTotalIncorrectCount;
    RecyclerView rvUsuarios;
    UsuarioAdapter adapter;

    DatabaseReference dbRoot;
    DatabaseReference playersRef;
    DatabaseReference usuariosRef;
    DatabaseReference usernamesRef;

    // UI Login
    LinearLayout layoutLoginManual; // Grupo de campos de login
    EditText etChildUsername, etChildPassword;
    Button btnShowChild, btnClearChild;
    Button btnVincularCodigo;
    Button btnDesvincular; // Nuevo botón

    // UI Datos Niño
    View cardChildStats;
    TextView tvChildName, tvChildAge, tvChildClams;
    TextView tvChildCorrectCount, tvChildIncorrectCount;

    // Controles Parentales
    MaterialCardView cardParentalControls;
    SwitchMaterial switchBlockTiktok, switchBlockYoutube, switchBlockInstagram;
    TextView tvUsageTiktok, tvUsageYoutube, tvUsageInstagram;
    DatabaseReference controlsRef;
    DatabaseReference usageRef;

    // Listeners
    private String currentChildUid = null;
    private ValueEventListener usageListener;
    private ValueEventListener statsListener;

    String googleApiKey;

    // Dummies
    private TextView tvPlayersCount;
    private TextView tvClamsTotal;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        FirebaseApp.initializeApp(this);
        EdgeToEdge.enable(this);
        setContentView(R.layout.activity_dashboard);

        // --- BINDS ---
        tvTotalCorrectCount = findViewById(R.id.tvTotalCorrectCount);
        tvTotalIncorrectCount = findViewById(R.id.tvTotalIncorrectCount);
        rvUsuarios = findViewById(R.id.rvUsuarios);

        layoutLoginManual = findViewById(R.id.layoutLoginManual);
        etChildUsername = findViewById(R.id.etChildUsername);
        etChildPassword = findViewById(R.id.etChildPassword);
        btnShowChild = findViewById(R.id.btnShowChild);
        btnClearChild = findViewById(R.id.btnClearChild);
        btnVincularCodigo = findViewById(R.id.btnVincularCodigo);
        btnDesvincular = findViewById(R.id.btnDesvincular);

        cardChildStats = findViewById(R.id.cardChildStats);
        tvChildName = findViewById(R.id.tvChildName);
        tvChildAge = findViewById(R.id.tvChildAge);
        tvChildClams = findViewById(R.id.tvChildClams);
        tvChildCorrectCount = findViewById(R.id.tvChildCorrectCount);
        tvChildIncorrectCount = findViewById(R.id.tvChildIncorrectCount);

        cardParentalControls = findViewById(R.id.cardParentalControls);
        switchBlockTiktok = findViewById(R.id.switchBlockTiktok);
        switchBlockYoutube = findViewById(R.id.switchBlockYoutube);
        switchBlockInstagram = findViewById(R.id.switchBlockInstagram);
        tvUsageTiktok = findViewById(R.id.tvUsageTiktok);
        tvUsageYoutube = findViewById(R.id.tvUsageYoutube);
        tvUsageInstagram = findViewById(R.id.tvUsageInstagram);

        adapter = new UsuarioAdapter();
        rvUsuarios.setLayoutManager(new LinearLayoutManager(this));
        rvUsuarios.setAdapter(adapter);

        // Firebase
        dbRoot = FirebaseDatabase.getInstance().getReference();
        playersRef = dbRoot.child("players");
        usuariosRef = dbRoot.child("usuarios");
        usernamesRef = dbRoot.child("usernames");

        int keyId = getResources().getIdentifier("google_api_key", "string", getPackageName());
        googleApiKey = (keyId != 0) ? getString(keyId) : "";

        // Inicial UI
        cardChildStats.setVisibility(View.GONE);
        cardParentalControls.setVisibility(View.GONE);
        rvUsuarios.setVisibility(View.GONE);
        btnDesvincular.setVisibility(View.GONE); // Oculto al inicio

        // --- 1. PERSISTENCIA: Verificar si ya hay un niño vinculado ---
        String savedUid = getSharedPreferences("app_data", MODE_PRIVATE).getString("child_uid", null);
        if (savedUid != null) {
            validateAndShowByUid(savedUid);
        } else {
            // Si no hay guardado, revisar si vino por Intent
            if (getIntent() != null && getIntent().hasExtra("uid")) {
                String uidIntent = getIntent().getStringExtra("uid");
                if (uidIntent != null && !uidIntent.isEmpty()) {
                    validateAndShowByUid(uidIntent);
                }
            }
        }

        // --- 2. SOLICITAR NOTIFICACIONES (Solo si es necesario y no se tiene) ---
        if (Build.VERSION.SDK_INT >= 33) {
            if (ContextCompat.checkSelfPermission(this, Manifest.permission.POST_NOTIFICATIONS) != PackageManager.PERMISSION_GRANTED) {
                ActivityCompat.requestPermissions(this, new String[]{Manifest.permission.POST_NOTIFICATIONS}, 101);
            }
        }

        // Listeners
        btnShowChild.setOnClickListener(v -> {
            String u = etChildUsername.getText().toString().trim();
            String p = etChildPassword.getText().toString();
            if (!u.isEmpty() && !p.isEmpty()) lookupUidAndVerify(u.trim().toLowerCase(), p);
        });

        btnVincularCodigo.setOnClickListener(v -> mostrarDialogoVinculacion());

        btnClearChild.setOnClickListener(v -> {
            etChildUsername.setText(""); etChildPassword.setText("");
        });

        // BOTÓN DESVINCULAR
        btnDesvincular.setOnClickListener(v -> desvincularUsuario());
    }

    private void desvincularUsuario() {
        // Borrar de preferencias
        getSharedPreferences("app_data", MODE_PRIVATE).edit().remove("child_uid").apply();

        // Limpiar UI y listeners
        limpiarDashboard();

        // Restaurar vista de login
        layoutLoginManual.setVisibility(View.VISIBLE);
        btnDesvincular.setVisibility(View.GONE);

        Toast.makeText(this, "Desvinculado correctamente", Toast.LENGTH_SHORT).show();
    }

    private void limpiarDashboard() {
        // Detener listeners
        if (currentChildUid != null) {
            if (usageRef != null && usageListener != null) usageRef.removeEventListener(usageListener);
            if (playersRef.child(currentChildUid) != null && statsListener != null) {
                playersRef.child(currentChildUid).removeEventListener(statsListener);
            }
        }
        currentChildUid = null;

        // Ocultar paneles de datos
        cardChildStats.setVisibility(View.GONE);
        cardParentalControls.setVisibility(View.GONE);

        tvChildClams.setText("0");
        tvChildName.setText(getString(R.string.label_name_empty));
        tvChildAge.setText(getString(R.string.label_age_empty));
        if(tvChildCorrectCount != null) tvChildCorrectCount.setText("0");
        if(tvChildIncorrectCount != null) tvChildIncorrectCount.setText("0");
        if(tvTotalCorrectCount != null) tvTotalCorrectCount.setText("0");
        if(tvTotalIncorrectCount != null) tvTotalIncorrectCount.setText("0");

        if(switchBlockTiktok != null) switchBlockTiktok.setChecked(false);
        if(switchBlockYoutube != null) switchBlockYoutube.setChecked(false);
        if(switchBlockInstagram != null) switchBlockInstagram.setChecked(false);
    }

    private void mostrarDialogoVinculacion() {
        AlertDialog.Builder builder = new AlertDialog.Builder(this);
        builder.setTitle("Vincular con Código");
        builder.setMessage("Ingresa el código de Lumina:");
        final EditText input = new EditText(this);
        input.setInputType(InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_CAP_CHARACTERS);
        builder.setView(input);
        builder.setPositiveButton("Vincular", (dialog, which) -> {
            String c = input.getText().toString().toUpperCase().trim().replace("-", "");
            if (!c.isEmpty()) procesarCodigoVinculacion(c);
        });
        builder.setNegativeButton("Cancelar", (d, w) -> d.cancel());
        builder.show();
    }

    private void procesarCodigoVinculacion(String codigo) {
        if (FirebaseAuth.getInstance().getCurrentUser() == null) {
            Toast.makeText(this, "Inicia sesión como padre primero.", Toast.LENGTH_LONG).show(); return;
        }
        String padreUid = FirebaseAuth.getInstance().getCurrentUser().getUid();
        dbRoot.child("pairing_codes").child(codigo).get().addOnCompleteListener(task -> {
            if (!task.isSuccessful() || !task.getResult().exists()) {
                Toast.makeText(this, "Código no encontrado.", Toast.LENGTH_LONG).show(); return;
            }
            String hijoUid = String.valueOf(task.getResult().getValue());
            dbRoot.child("vinculos").child(padreUid).child(hijoUid).setValue(true)
                    .addOnSuccessListener(a -> {
                        Toast.makeText(this, "¡Vinculado!", Toast.LENGTH_SHORT).show();
                        dbRoot.child("pairing_codes").child(codigo).removeValue();
                        validateAndShowByUid(hijoUid);
                    });
        });
    }

    private void lookupUidAndVerify(String username, String password) {
        usernamesRef.child(username).get().addOnCompleteListener(task -> {
            if (!task.isSuccessful() || !task.getResult().exists()) {
                Toast.makeText(this, "Usuario no encontrado", Toast.LENGTH_SHORT).show(); return;
            }
            String uid = task.getResult().getValue(String.class);
            String email = username + "@lumina.local";
            verifyPasswordWithRest(email, password, (ok, result) -> runOnUiThread(() -> {
                if (ok) {
                    String padre = FirebaseAuth.getInstance().getCurrentUser() != null ? FirebaseAuth.getInstance().getCurrentUser().getUid() : null;
                    if (padre != null) dbRoot.child("vinculos").child(padre).child(uid).setValue(true).addOnSuccessListener(a -> validateAndShowByUid(uid));
                } else Toast.makeText(this, "Contraseña incorrecta", Toast.LENGTH_SHORT).show();
            }));
        });
    }

    private void validateAndShowByUid(String uid) {
        if (uid == null || uid.isEmpty()) return;
        limpiarDashboard();
        currentChildUid = uid;

        // 1. Guardar persistencia
        getSharedPreferences("app_data", MODE_PRIVATE).edit().putString("child_uid", uid).apply();

        // 2. Ajustar UI: Ocultar login, mostrar desvincular
        layoutLoginManual.setVisibility(View.GONE);
        btnDesvincular.setVisibility(View.VISIBLE);

        // 3. Listeners
        statsListener = new ValueEventListener() {
            @Override
            public void onDataChange(@NonNull DataSnapshot snapshot) {
                if (!snapshot.exists()) return;
                long clams = 0;
                if (snapshot.child("clams").exists()) clams = Long.parseLong(String.valueOf(snapshot.child("clams").getValue()));
                tvChildClams.setText(String.valueOf(clams));

                long tc = 0, ti = 0; // Totales
                if (snapshot.child("quizStats").child("correctas").exists()) tc = Long.parseLong(String.valueOf(snapshot.child("quizStats").child("correctas").getValue()));
                if (snapshot.child("quizStats").child("incorrectas").exists()) ti = Long.parseLong(String.valueOf(snapshot.child("quizStats").child("incorrectas").getValue()));

                long sc = 0, si = 0; // Sesión
                if (snapshot.child("sessionStats").child("correctas").exists()) sc = Long.parseLong(String.valueOf(snapshot.child("sessionStats").child("correctas").getValue()));
                if (snapshot.child("sessionStats").child("incorrectas").exists()) si = Long.parseLong(String.valueOf(snapshot.child("sessionStats").child("incorrectas").getValue()));

                if (tvChildCorrectCount != null) tvChildCorrectCount.setText(String.valueOf(sc));
                if (tvChildIncorrectCount != null) tvChildIncorrectCount.setText(String.valueOf(si));
                if (tvTotalCorrectCount != null) tvTotalCorrectCount.setText(String.valueOf(tc));
                if (tvTotalIncorrectCount != null) tvTotalIncorrectCount.setText(String.valueOf(ti));

                if (snapshot.child("nombre").exists()) tvChildName.setText(String.valueOf(snapshot.child("nombre").getValue()));
            }
            @Override public void onCancelled(@NonNull DatabaseError error) {}
        };
        playersRef.child(uid).addValueEventListener(statsListener);

        usuariosRef.child(uid).child("profile").get().addOnCompleteListener(task -> {
            if (task.isSuccessful() && task.getResult().exists()) {
                DataSnapshot prof = task.getResult();
                if (prof.child("nombre").exists()) tvChildName.setText(String.valueOf(prof.child("nombre").getValue()));
                int anio = 0;
                if (prof.child("anioNacimiento").exists()) {
                    try { anio = Integer.parseInt(String.valueOf(prof.child("anioNacimiento").getValue())); } catch(Exception e){}
                }
                if (anio > 0) tvChildAge.setText(getString(R.string.label_age_placeholder, anio));
            }
            cardChildStats.setVisibility(View.VISIBLE);
            cardParentalControls.setVisibility(View.VISIBLE);
            setupFirebaseControls(uid);
        });
    }

    private void setupFirebaseControls(String uid) {
        controlsRef = usuariosRef.child(uid).child("controles");
        usageRef = playersRef.child(uid).child("usage").child("apps");

        configureSwitch(controlsRef, "block_tiktok", switchBlockTiktok);
        configureSwitch(controlsRef, "block_youtube", switchBlockYoutube);
        configureSwitch(controlsRef, "block_instagram", switchBlockInstagram);

        usageListener = new ValueEventListener() {
            @Override
            public void onDataChange(@NonNull DataSnapshot snapshot) {
                long tiktok = getLong(snapshot, "com_tiktok");
                long youtube = getLong(snapshot, "com_youtube");
                long insta = getLong(snapshot, "com_instagram");
                tvUsageTiktok.setText(formatSeconds(tiktok));
                tvUsageYoutube.setText(formatSeconds(youtube));
                tvUsageInstagram.setText(formatSeconds(insta));
            }
            @Override public void onCancelled(@NonNull DatabaseError error) {}
        };
        usageRef.addValueEventListener(usageListener);
    }

    private void configureSwitch(DatabaseReference ref, String key, SwitchMaterial sw) {
        ref.child(key).get().addOnCompleteListener(t -> {
            if (t.isSuccessful() && t.getResult().exists()) {
                Boolean val = t.getResult().getValue(Boolean.class);
                sw.setChecked(val != null && val);
            } else sw.setChecked(false);

            sw.setOnCheckedChangeListener((v, isChecked) -> {
                if(v.isPressed()) ref.child(key).setValue(isChecked);
            });
        });
    }

    private long getLong(DataSnapshot snap, String key) {
        if (snap.child(key).exists()) return Long.parseLong(String.valueOf(snap.child(key).getValue()));
        return 0;
    }

    private String formatSeconds(long seconds) {
        long hours = seconds / 3600; long minutes = (seconds % 3600) / 60;
        if (hours > 0) return String.format(Locale.getDefault(), "Tiempo: %dh %dm", hours, minutes);
        return String.format(Locale.getDefault(), "Tiempo: %dm", minutes);
    }

    private void verifyPasswordWithRest(String email, String password, RestVerifyCallback cb) {
        if (googleApiKey == null || googleApiKey.isEmpty()) { cb.onResult(false, "API Key"); return; }
        new Thread(() -> {
            try {
                URL url = new URL("https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=" + googleApiKey);
                HttpURLConnection conn = (HttpURLConnection) url.openConnection();
                conn.setRequestMethod("POST");
                conn.setRequestProperty("Content-Type", "application/json");
                conn.setDoOutput(true);
                JSONObject p = new JSONObject();
                p.put("email", email); p.put("password", password); p.put("returnSecureToken", true);
                try(OutputStream os = conn.getOutputStream()) { os.write(p.toString().getBytes(StandardCharsets.UTF_8)); }
                if (conn.getResponseCode() == 200) {
                    try(BufferedReader br = new BufferedReader(new InputStreamReader(conn.getInputStream()))) {
                        StringBuilder sb = new StringBuilder(); String l;
                        while ((l = br.readLine()) != null) sb.append(l);
                        cb.onResult(true, new JSONObject(sb.toString()).optString("localId"));
                    }
                } else cb.onResult(false, "Error");
            } catch (Exception e) { cb.onResult(false, "Ex"); }
        }).start();
    }
}