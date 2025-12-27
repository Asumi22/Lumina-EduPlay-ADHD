package com.example.atentoyactivo;

import android.content.Intent;
import android.os.Bundle;
import android.widget.Button;
import android.widget.EditText;
import android.widget.Toast;

import androidx.activity.EdgeToEdge;
import androidx.appcompat.app.AppCompatActivity;

import com.google.firebase.FirebaseApp;
import com.google.firebase.auth.FirebaseAuth;

public class LoginActivity extends AppCompatActivity {
    EditText etUsuario, etPwd;
    Button btnIngresar, btnRegistrar;
    int intentos;
    FirebaseAuth mAuth;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        FirebaseApp.initializeApp(this);

        EdgeToEdge.enable(this);
        setContentView(R.layout.activity_login);

        etUsuario = findViewById(R.id.txtCorreoVal);
        etPwd = findViewById(R.id.txtPwdVal);
        btnIngresar = findViewById(R.id.btnIngresar);
        btnRegistrar = findViewById(R.id.btnRegistrar);
        intentos = 0;

        mAuth = FirebaseAuth.getInstance();

        btnIngresar.setOnClickListener(view -> {
            if (intentos == 3) {
                finish();
                return;
            }

            String correo = etUsuario.getText().toString().trim();
            String pass = etPwd.getText().toString().trim();

            if (correo.isEmpty() || pass.isEmpty()) {
                Toast.makeText(this, "Llene todos los campos", Toast.LENGTH_LONG).show();
                return;
            }

            // Admin local (opcional)
            if (correo.equals("admin@gmail.com") && pass.equals("1234")) {
                Toast.makeText(this, "INICIO EXITOSO", Toast.LENGTH_LONG).show();
                intentos = 0;

                // Va al escudo, luego dashboard
                Intent intent = new Intent(this, MainActivity.class);
                intent.putExtra("nextScreen", "dashboard");
                intent.putExtra("nombre", "admin");
                intent.putExtra("correo", "admin@gmail.com");
                limpiar();
                startActivity(intent);
                return;
            }

            // Firebase login
            mAuth.signInWithEmailAndPassword(correo, pass)
                    .addOnCompleteListener(task -> {
                        if (task.isSuccessful()) {
                            Toast.makeText(this, "INICIO EXITOSO", Toast.LENGTH_LONG).show();
                            intentos = 0;

                            // Va al escudo, luego dashboard
                            Intent intent = new Intent(this, MainActivity.class);
                            intent.putExtra("nextScreen", "dashboard");
                            intent.putExtra("correo", correo);
                            limpiar();
                            startActivity(intent);
                        } else {
                            intentos++;
                            String msg = (task.getException() != null && task.getException().getMessage() != null)
                                    ? task.getException().getMessage()
                                    : "Usuario o contraseña incorrectos";
                            Toast.makeText(this,
                                    "USUARIO O CONTRASEÑA INCORRECTO. " + msg + " Intentos restantes " + (3 - intentos) + ".",
                                    Toast.LENGTH_LONG).show();
                        }
                    })
                    .addOnFailureListener(e -> {
                        String msg = (e != null && e.getMessage() != null) ? e.getMessage() : "Error desconocido";
                        Toast.makeText(this, "Error: " + msg, Toast.LENGTH_LONG).show();
                    });
        });

        btnRegistrar.setOnClickListener(v -> {
            startActivity(new Intent(this, RegistroActivity.class));
            limpiar();
        });
    }

    private void limpiar() {
        etUsuario.setText("");
        etPwd.setText("");
    }
}

