using UnityEngine;
using UnityEngine.EventSystems; // Necesario para los interfaces de Puntero
using UnityEngine.UI;

// Implementamos las interfaces para "Tocar", "Soltar" y "Arrastrar"
public class JoystickControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Referencias de UI")]
    [Tooltip("La imagen de fondo del joystick (el área circular)")]
    public Image joystickBackground;

    [Tooltip("La imagen del 'mango' del joystick (la bolita que se mueve)")]
    public Image joystickHandle;

    private Vector2 inputVector;
    private float backgroundRadius;

    void Start()
    {
        // Calculamos el radio del fondo para saber el límite del 'handle'
        backgroundRadius = joystickBackground.rectTransform.sizeDelta.x / 2f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData); // Tratar el primer "toque" como un "arrastre"
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 touchPosition;

        // Convertimos la posición de la pantalla (donde toca el dedo)
        // a una posición local dentro del 'joystickBackground'
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground.rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out touchPosition))
        {
            // Limitamos el vector para que el "handle" no se salga del fondo
            inputVector = Vector2.ClampMagnitude(touchPosition, backgroundRadius);

            // Movemos el "handle" a la posición del dedo (limitada)
            joystickHandle.rectTransform.anchoredPosition = inputVector;

            // --- ¡LA PARTE CLAVE! ---
            // Actualizamos el script MobileInput.cs con el valor horizontal (-1 a 1)
            if (MobileInput.Instance != null)
            {
                // Dividimos la posición X por el radio para obtener un valor
                // normalizado entre -1 (izquierda) y 1 (derecha).
                MobileInput.Instance.horizontal = inputVector.x / backgroundRadius;
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Resetea el "handle" al centro
        joystickHandle.rectTransform.anchoredPosition = Vector2.zero;

        // --- ¡LA PARTE CLAVE! ---
        // Resetea el movimiento
        if (MobileInput.Instance != null)
        {
            MobileInput.Instance.horizontal = 0f;
        }
    }
}