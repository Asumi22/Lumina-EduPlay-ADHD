using UnityEngine;

/// <summary>
/// CangrejoEnemy: patrulla horizontalmente entre leftLimitX y rightLimitX.
/// - Asigna el tag del GameObject a "Damage" en el inspector para que el player reciba daño.
/// - Recomendado: marcar el Collider2D del cangrejo como "Is Trigger" si quieres que el player detecte el trigger (OnTriggerEnter2D).
/// </summary>
public class CangrejoEnemy : MonoBehaviour
{
    public float speed = 2f;

    // Límites X world coordinates (ajusta en inspector)
    public float leftLimitX = 0f;
    public float rightLimitX = 0f;

    private bool movingRight = true;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;

    // cache para saber si existe param 'Speed'
    private bool hasSpeedParam = false;
    private bool hasCheckedParams = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        // Si no cambiaste los límites en inspector, toma la posición inicial y mueve en ±4 unidades
        if (Mathf.Approximately(leftLimitX, 0f) && Mathf.Approximately(rightLimitX, 0f))
        {
            leftLimitX = transform.position.x - 4f;
            rightLimitX = transform.position.x + 4f;
        }
    }

    void Update()
    {
        if (rb == null) return;

        float vx = movingRight ? speed : -speed;

        // Usar linearVelocity en lugar de velocity (tu preferencia)
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

        // Flip visual si hay SpriteRenderer
        if (sr != null)
            sr.flipX = !movingRight; // ajústalo si tu sprite está invertido

        // Cambiar dirección al alcanzar límites
        if (movingRight && transform.position.x >= rightLimitX)
        {
            movingRight = false;
        }
        else if (!movingRight && transform.position.x <= leftLimitX)
        {
            movingRight = true;
        }

        // chequeo de parámetros (sólo una vez al inicio)
        if (animator != null && !hasCheckedParams)
        {
            hasCheckedParams = true;
            foreach (var p in animator.parameters)
            {
                if (p.name == "Speed" && p.type == AnimatorControllerParameterType.Float)
                {
                    hasSpeedParam = true;
                    break;
                }
            }
        }

        if (animator != null && hasSpeedParam)
        {
            animator.SetFloat("Speed", Mathf.Abs(vx));
        }
    }

    // Si quieres que el cangrejo invierta dirección al chocar con paredes/obstaculos,
    // asegúrate de que esos objetos tengan tag "Obstacle" y colliders sin IsTrigger.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            movingRight = !movingRight;
        }
    }

    // Método público para matar al cangrejo (si lo quieres desde otra lógica)
    public void Die()
    {
        if (animator != null)
        {
            animator.Play("death", 0, 0f);
        }
        // Destruir tras pequeña demora para ver la animación
        Destroy(gameObject, 0.6f);
    }
}



