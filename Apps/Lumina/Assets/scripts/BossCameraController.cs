using UnityEngine;
using System.Collections;

public class BossCameraController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aquí el objeto del Jugador (Vaquita)")]
    public Transform player;
    [Tooltip("Arrastra aquí el objeto del Jefe (BossFinal)")]
    public Transform boss;

    [Header("Configuración de Zoom")]
    [Tooltip("El tamaño normal de la cámara (ej. 5 o 6)")]
    public float normalZoom = 6f;
    [Tooltip("El tamaño de la cámara durante la pelea (ej. 8 o 9)")]
    public float bossZoom = 8.5f;
    [Tooltip("La distancia a la que el jefe activa el zoom")]
    public float triggerDistance = 15f;
    [Tooltip("Qué tan rápido hace el zoom (más alto = más rápido)")]
    public float zoomSpeed = 2f;

    private Camera mainCamera;
    private bool isBossActive = false;

    void Start()
    {
        // Encuentra la cámara principal automáticamente
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[BossCameraController] ¡No se encontró la 'Main Camera' en la escena!");
            this.enabled = false;
            return;
        }

        // Asegura que la cámara sea ortográfica (para 2D)
        if (!mainCamera.orthographic)
        {
            Debug.LogError("[BossCameraController] ¡La cámara principal no es Ortográfica!");
            this.enabled = false;
        }

        // Inicia con el zoom normal
        mainCamera.orthographicSize = normalZoom;
    }

    void Update()
    {
        // Si el jugador o el jefe no existen (ej. jefe muerto), vuelve al zoom normal
        if (player == null || boss == null)
        {
            isBossActive = false;
        }
        else
        {
            // Mide la distancia al jefe
            float distance = Vector2.Distance(player.position, boss.position);

            // Activa la 'zona de jefe' si el jugador está cerca
            if (distance < triggerDistance)
            {
                isBossActive = true;
            }
            // Opcional: Desactivar si el jugador se aleja mucho
            // else if (distance > triggerDistance + 5f) // +5f de margen
            // {
            //     isBossActive = false;
            // }
        }

        // Aplica el zoom suavemente
        float targetZoom = isBossActive ? bossZoom : normalZoom;

        mainCamera.orthographicSize = Mathf.Lerp(
            mainCamera.orthographicSize,
            targetZoom,
            Time.deltaTime * zoomSpeed
        );
    }
}