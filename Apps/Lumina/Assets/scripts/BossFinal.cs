using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class BossFinal : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform groundCheck;
    public LayerMask groundLayer;
    // Variable para la puerta final
    public DoorController exitDoor;

    [Header("Prefabs")]
    public GameObject clamPrefab;
    public GameObject crabPrefab;
    public GameObject urchinPrefab;
    public GameObject crabVanishEffect;

    [Header("Behavior")]
    public float aggroRange = 15f;
    public float engageTime = 6f;
    public float retreatTime = 4f;
    public float moveSpeed = 2.2f;
    public float retreatSpeed = 2.2f;

    [Header("Jump Settings")]
    public float jumpForce = 15f;
    public float jumpInterval = 1f;
    private float jumpTimer = 0f;

    [Header("Spawning while following")]
    public float clamSpawnInterval = 3.0f;
    public float enemySpawnInterval = 5.0f;
    public float urchinSpawnInterval = 4.0f;
    public Vector2 spawnOffset = new Vector2(1.5f, 0.2f);

    [Header("Urchin behavior")]
    public float urchinShootForce = 4.5f;
    public float urchinLifetime = 5f;
    public float urchinSpreadAngle = 25f;
    public float urchinGravityScale = 0.3f;

    [Header("Crab behavior")]
    public float crabShootForce = 4.0f;
    public float crabSpreadAngle = 15f;
    public float crabGravityScale = 0.5f;
    public float crabLifetime = 8f;

    [Header("Clam behavior (Shooting)")]
    public float clamShootForce = 5.0f;
    public float clamSpreadAngle = 20f;
    public float clamGravityScale = 0.4f;

    [Header("Victory condition")]
    public int requiredCorrectAnswers = 6;
    public float destroyDelayAfterDeath = 1.2f;

    [Header("GroundCheck")]
    public float groundCheckRadius = 0.12f;

    Rigidbody2D rb;
    Animator animator;
    SpriteRenderer sr;

    bool cycleRunning = false;
    enum Phase { None, Follow, Retreat }
    Phase currentPhase = Phase.None;

    float clamTimer = 0f;
    float enemyTimer = 0f;
    float urchinTimer = 0f;

    int correctAnswers = 0;
    private bool isDead = false;

    int hSpeed, hVertical, hGrounded, hJumpTrig, hDie;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        rb.freezeRotation = true;
        rb.gravityScale = 2.5f;
        hSpeed = Animator.StringToHash("Speed");
        hVertical = Animator.StringToHash("VerticalSpeed");
        hGrounded = Animator.StringToHash("IsGrounded");
        hJumpTrig = Animator.StringToHash("JumpTrigger");
        hDie = Animator.StringToHash("Die");
    }

    void Start()
    {
        if (rb.interpolation == RigidbodyInterpolation2D.None)
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {
        if (isDead || player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= aggroRange && !cycleRunning)
        {
            StartCoroutine(FollowRetreatCycle());
        }

        Vector2 linVel = rb.linearVelocity;
        float hsp = linVel.x;
        float vsp = linVel.y;
        animator.SetFloat(hSpeed, Mathf.Abs(hsp));
        animator.SetFloat(hVertical, vsp);
        animator.SetBool(hGrounded, IsGrounded());

        if (hsp > 0.10f) sr.flipX = false;
        else if (hsp < -0.10f) sr.flipX = true;

        if (currentPhase == Phase.Follow)
        {
            clamTimer += Time.deltaTime;
            enemyTimer += Time.deltaTime;
            urchinTimer += Time.deltaTime;
            jumpTimer += Time.deltaTime;

            if (clamTimer >= clamSpawnInterval) { SpawnClam(); clamTimer = 0f; }
            if (enemyTimer >= enemySpawnInterval) { SpawnCrab(); enemyTimer = 0f; }
            if (urchinTimer >= urchinSpawnInterval) { SpawnUrchin(); urchinTimer = 0f; }
            if (jumpTimer >= jumpInterval) { TryJump(); jumpTimer = 0f; }
        }
    }

    IEnumerator FollowRetreatCycle()
    {
        cycleRunning = true;
        currentPhase = Phase.Follow;
        float t = 0f;
        while (t < engageTime)
        {
            if (player == null || isDead) break;
            FollowPlayerFrame();
            t += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        currentPhase = Phase.Retreat;
        t = 0f;
        if (player != null)
        {
            float awaySign = Mathf.Sign(transform.position.x - player.position.x);
            if (Mathf.Abs(awaySign) < 0.2f) awaySign = (transform.position.x < player.position.x) ? -1f : 1f;

            while (t < retreatTime)
            {
                if (isDead) break;
                rb.linearVelocity = new Vector2(awaySign * retreatSpeed, rb.linearVelocity.y);
                t += Time.deltaTime;
                yield return null;
            }
        }
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        currentPhase = Phase.None;
        cycleRunning = false;
        clamTimer = 0f;
        enemyTimer = 0f;
        urchinTimer = 0f;
        jumpTimer = 0f;
    }

    void FollowPlayerFrame()
    {
        if (player == null || isDead) return;
        float dir = Mathf.Sign(player.position.x - transform.position.x);
        float vx = dir * moveSpeed;
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
    }

    void TryJump()
    {
        if (!IsGrounded() || isDead) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        animator.SetTrigger(hJumpTrig);
    }

    void SpawnClam()
    {
        if (clamPrefab == null || player == null) return;
        Vector3 spawnPos = transform.position + (Vector3)(spawnOffset * (sr.flipX ? -1f : 1f));
        GameObject g = Instantiate(clamPrefab, spawnPos, Quaternion.identity);
        if (g != null && g.tag == "Untagged") g.tag = "SpecialClam";
        Collider2D col = g.GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        Rigidbody2D cRb = g.GetComponent<Rigidbody2D>();
        if (cRb != null)
        {
            cRb.bodyType = RigidbodyType2D.Dynamic;
            cRb.gravityScale = clamGravityScale;
            Vector2 toPlayer = (player.position - transform.position).normalized;
            float angleOffset = Random.Range(-clamSpreadAngle, clamSpreadAngle);
            Vector2 shootDir = Quaternion.Euler(0, 0, angleOffset) * toPlayer;
            cRb.AddForce(shootDir * clamShootForce, ForceMode2D.Impulse);
        }
    }

    void SpawnCrab()
    {
        if (crabPrefab == null || player == null) return;
        Vector3 spawnPos = transform.position + new Vector3(spawnOffset.x * (sr.flipX ? -1f : 1f), spawnOffset.y, 0f);
        GameObject e = Instantiate(crabPrefab, spawnPos, Quaternion.identity);

        if (e != null)
        {
            e.tag = "Damage";
            Rigidbody2D cRb = e.GetComponent<Rigidbody2D>();
            if (cRb != null)
            {
                cRb.bodyType = RigidbodyType2D.Dynamic;
                cRb.gravityScale = crabGravityScale;
                Vector2 toPlayer = (player.position - transform.position).normalized;
                float angleOffset = Random.Range(-crabSpreadAngle, crabSpreadAngle);
                Vector2 shootDir = Quaternion.Euler(0, 0, angleOffset) * toPlayer;
                cRb.AddForce(shootDir * crabShootForce, ForceMode2D.Impulse);
            }
            StartCoroutine(DestroyCrabWithEffect(e, crabLifetime));
        }
    }

    IEnumerator DestroyCrabWithEffect(GameObject crab, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (crab != null)
        {
            if (crabVanishEffect != null)
                Instantiate(crabVanishEffect, crab.transform.position, Quaternion.identity);
            Destroy(crab);
        }
    }

    void SpawnUrchin()
    {
        if (urchinPrefab == null || player == null) return;
        Vector3 spawnPos = transform.position + new Vector3(spawnOffset.x * (sr.flipX ? -1f : 1f), spawnOffset.y + 0.5f, 0f);
        GameObject u = Instantiate(urchinPrefab, spawnPos, Quaternion.identity);

        Rigidbody2D uRb = u.GetComponent<Rigidbody2D>();
        if (uRb != null)
        {
            uRb.bodyType = RigidbodyType2D.Dynamic;
            uRb.gravityScale = urchinGravityScale;
            Vector2 toPlayer = (player.position - transform.position).normalized;
            float angleOffset = Random.Range(-urchinSpreadAngle, urchinSpreadAngle);
            Vector2 shootDir = Quaternion.Euler(0, 0, angleOffset) * toPlayer;
            uRb.AddForce(shootDir * urchinShootForce, ForceMode2D.Impulse);
        }
        Destroy(u, urchinLifetime);
    }

    bool IsGrounded()
    {
        if (groundCheck == null) return false;
        Collider2D c = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        return c != null;
    }

    public void RegisterCorrectAnswer()
    {
        if (isDead) return;
        correctAnswers++;
        Debug.Log($"[BossFinal] Correct answers received: {correctAnswers}/{requiredCorrectAnswers}");
        if (correctAnswers >= requiredCorrectAnswers)
        {
            StartCoroutine(DoDeathAndDestroy());
        }
    }

    IEnumerator DoDeathAndDestroy()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        animator.SetTrigger(hDie);
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        cycleRunning = false;
        currentPhase = Phase.None;
        StopAllCoroutines();

        // --- ABRIR LA PUERTA AL MORIR ---
        if (exitDoor != null)
        {
            Debug.Log("[BossFinal] Jefe derrotado. Abriendo puerta de salida.");
            exitDoor.UnlockAndOpen();
        }

        yield return new WaitForSeconds(destroyDelayAfterDeath);
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        if (groundCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}