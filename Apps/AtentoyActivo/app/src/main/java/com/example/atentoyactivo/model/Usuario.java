package com.example.atentoyactivo.model;

public class Usuario {
    private String uid;
    private String nombre;
    private String correo;
    private String contrasena;
    private String role;
    private int anioNacimiento; // en tu BD el campo puede ser 6 (edad) o año; ver nota abajo
    private long clams; // solo por si hay clams dentro de usuarios (fallback)

    public Usuario() { }

    public Usuario(String nombre, String correo, String contrasena) {
        this.nombre = nombre;
        this.correo = correo;
        this.contrasena = contrasena;
    }

    public String getUid() { return uid; }
    public void setUid(String uid) { this.uid = uid; }

    public String getNombre() { return nombre; }
    public void setNombre(String nombre) { this.nombre = nombre; }

    public String getCorreo() { return correo; }
    public void setCorreo(String correo) { this.correo = correo; }

    public String getContrasena() { return contrasena; }
    public void setContrasena(String contrasena) { this.contrasena = contrasena; }

    public String getRole() { return role; }
    public void setRole(String role) { this.role = role; }

    public int getAnioNacimiento() { return anioNacimiento; }
    public void setAnioNacimiento(int anioNacimiento) { this.anioNacimiento = anioNacimiento; }

    public long getClams() { return clams; }
    public void setClams(long clams) { this.clams = clams; }
}




