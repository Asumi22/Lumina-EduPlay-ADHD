package com.example.atentoyactivo;

import android.content.Intent;
import android.os.Bundle;
import android.widget.Button;
import android.widget.EditText;
import android.widget.Toast;
// Ya no necesitas 'import com.example.atentoyactivo.model.Usuario;'
// import com.example.atentoyactivo.model.Usuario;

import androidx.activity.EdgeToEdge;
import androidx.appcompat.app.AppCompatActivity;

import com.google.firebase.FirebaseApp;
import com.google.firebase.auth.FirebaseAuth;
import com.google.firebase.auth.FirebaseUser;
import com.google.firebase.database.DatabaseReference;
import com.google.firebase.database.FirebaseDatabase;

import java.util.HashMap;
import java.util.Map;

public class RegistroActivity extends AppCompatActivity {
    EditText txtNombre, txtMail, txtPwd, txtPwdConfirm;
    Button btnRegistrar;
    FirebaseAuth mAuth;
    DatabaseReference dbRef; // Esto seguirá apuntando a "usuarios"

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        // Inicializar Firebase (idempotente)
        FirebaseApp.initializeApp(this);

        EdgeToEdge.enable(this);
        setContentView(R.layout.activity_registro);

        // --- Corrección de IDs ---
        // Tus EditText estaban dentro de un TextInputLayout.
        // Debes obtener el ID del TextInputEditText interno, que son 'txtNombreVal', 'txtCorreoVal', etc.
        // Tu código original ya hacía esto, así que está correcto.
        txtNombre = findViewById(R.id.txtNombreVal);
        txtMail = findViewById(R.id.txtCorreoVal);
        txtPwd = findViewById(R.id.txtPwdVal);
        txtPwdConfirm = findViewById(R.id.txtPwdConfVal);
        btnRegistrar = findViewById(R.id.btnRegistrar);

        mAuth = FirebaseAuth.getInstance();
        dbRef = FirebaseDatabase.getInstance().getReference("usuarios"); // Referencia a /usuarios

        btnRegistrar.setOnClickListener(v -> {
            String nombre = txtNombre.getText().toString().trim();
            String mail = txtMail.getText().toString().trim();
            String pwd = txtPwd.getText().toString().trim();
            String pwdConf = txtPwdConfirm.getText().toString().trim();

            if (nombre.isEmpty()) {
                Toast.makeText(this, "Campo nombre vacío.", Toast.LENGTH_LONG).show();
                return;
            }
            if (mail.isEmpty()) {
                Toast.makeText(this, "Campo correo vacío.", Toast.LENGTH_LONG).show();
                return;
            }
            if (pwd.isEmpty() || pwd.length() < 6) { // Firebase requiere al menos 6 caracteres
                Toast.makeText(this, "La contraseña debe tener al menos 6 caracteres.", Toast.LENGTH_LONG).show();
                return;
            }
            if (!pwd.equals(pwdConf)) {
                Toast.makeText(this, "Las contraseñas deben coincidir.", Toast.LENGTH_LONG).show();
                return;
            }

            // Deshabilitar el botón para evitar doble clic
            btnRegistrar.setEnabled(false);

            // Crear usuario en Firebase Authentication
            mAuth.createUserWithEmailAndPassword(mail, pwd)
                    .addOnCompleteListener(task -> {
                        if (task.isSuccessful()) {
                            FirebaseUser firebaseUser = mAuth.getCurrentUser();
                            if (firebaseUser != null) {
                                String uid = firebaseUser.getUid();

                                // --- *** INICIO DE LA CORRECCIÓN *** ---

                                // 1. NO guardes la contraseña en la base de datos.
                                // 2. Crea un Map (diccionario) para guardar los datos del perfil.
                                // 3. Esta estructura debe coincidir con la que usa Lumina (FirebaseInit.cs)
                                Map<String, Object> profileData = new HashMap<>();
                                profileData.put("nombre", nombre);
                                profileData.put("correo", mail);
                                profileData.put("role", "parent"); // Rol para identificar a este usuario
                                profileData.put("En_linea", 0); // Estado inicial
                                profileData.put("Num_ingresos", 0); // Contador inicial
                                // 'anioNacimiento' no se pide en este formulario, así que se omite

                                // 4. Apunta a la ruta correcta: /usuarios/{uid}/profile
                                // dbRef es "usuarios", así que añadimos .child(uid).child("profile")
                                dbRef.child(uid).child("profile").setValue(profileData)
                                        .addOnSuccessListener(aVoid -> {
                                            // ¡ÉXITO! Auth y BD creados.
                                            Toast.makeText(this, "Usuario registrado correctamente.", Toast.LENGTH_LONG).show();
                                            Intent intent = new Intent(this, LoginActivity.class);
                                            // Limpia las actividades anteriores para que no pueda "volver" al registro
                                            intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
                                            startActivity(intent);
                                            finish(); // Cierra esta actividad
                                        })
                                        .addOnFailureListener(e -> {
                                            // Este era tu error. El Auth SÍ funcionó, pero la BD NO.
                                            String msg = (e.getMessage() != null) ? e.getMessage() : "Error al guardar en BD";
                                            Toast.makeText(this, "Error al guardar perfil: " + msg, Toast.LENGTH_LONG).show();
                                            // Opcional: podrías borrar el usuario de Auth si la BD falla
                                            // firebaseUser.delete();
                                            btnRegistrar.setEnabled(true); // Reactivar botón si falla
                                        });
                                // --- *** FIN DE LA CORRECCIÓN *** ---
                            } else {
                                Toast.makeText(this, "Registro creado, pero no se obtuvo UID.", Toast.LENGTH_LONG).show();
                                btnRegistrar.setEnabled(true); // Reactivar botón
                            }
                        } else {
                            // La creación del usuario en Auth falló (ej. email ya existe)
                            String msg = (task.getException() != null && task.getException().getMessage() != null)
                                    ? task.getException().getMessage()
                                    : "Error desconocido al registrar";
                            Toast.makeText(this, "Error al registrar: " + msg, Toast.LENGTH_LONG).show();
                            btnRegistrar.setEnabled(true); // Reactivar botón
                        }
                    });
            // El .addOnFailureListener original era redundante con el de .addOnCompleteListener
        });
    }

    public void limpiar() {
        txtNombre.setText("");
        txtMail.setText("");
        txtPwd.setText("");
        txtPwdConfirm.setText("");
    }
}