using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Events;

// Firebase solo en Android
#if !UNITY_WEBGL
using Firebase.Extensions;
using Firebase.Database;
#endif

public class VaquitaPlayer : MonoBehaviour
{
    public float speed = 5;
    private Rigidbody2D rb2D;
    private float move;

    [Header("Jump Settings")]
    public float jumpForce = 4;
    public float doubleJumpForce = 3.5f;
    public int maxJumps = 2;
    private int jumpsLeft;

    private bool isGrounded;
    public Transform groundCheck;
    public float groundRadius = 0.1f;
    public LayerMask groundLayer;

    private Animator animator;
    private int coins;
    public TMP_Text textCoins;
    private int specialClams;
    public TMP_Text textSpecialClam;

    public UnityEvent onSpecialClamCollected;
    private bool isDead = false;

    [SerializeField] private FirebaseInit firebaseInit;
    private bool firebaseNullWarned = false;
    public float authWaitTimeout = 10f;

    private static bool hasPlayedWelcome = false;

    void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        jumpsLeft = maxJumps;

        if (firebaseInit == null)
        {
            firebaseInit = FirebaseInit.EnsureInstance();
        }

        coins = 0;
        specialClams = 0;
        if (textCoins != null) textCoins.text = coins.ToString();
        if (textSpecialClam != null) textSpecialClam.text = specialClams.ToString();

        if (firebaseInit != null)
        {
            StartCoroutine(WaitForAuthThenResetClams(authWaitTimeout));
        }

        if (AudioManager.Instance != null && !hasPlayedWelcome)
        {
            AudioManager.Instance.PlayWelcomeVoice();
            hasPlayedWelcome = true;
        }
    }

    private FirebaseInit GetFirebase()
    {
        if (firebaseInit != null) return firebaseInit;
        if (FirebaseInit.Instance != null) { firebaseInit = FirebaseInit.Instance; return firebaseInit; }
        firebaseInit = FirebaseInit.EnsureInstance();
        if (firebaseInit != null) return firebaseInit;
        if (!firebaseNullWarned) { firebaseNullWarned = true; }
        return null;
    }

    private IEnumerator WaitForAuthThenResetClams(float timeoutSeconds)
    {
        // En WebGL no hacemos nada de esto
#if UNITY_WEBGL
        yield break;
#endif

#if !UNITY_WEBGL
        float start = Time.realtimeSinceStartup;
        bool didAuth = false;
        while (Time.realtimeSinceStartup - start < timeoutSeconds)
        {
            var fiCheck = GetFirebase();
            if (fiCheck != null && fiCheck.Auth != null && fiCheck.CurrentUser != null)
            {
                didAuth = true;
                break;
            }
            yield return null;
        }
        var firebase = GetFirebase();
        if (firebase == null) yield break;

        if (didAuth && firebase.CurrentUser != null)
        {
            string uid = firebase.CurrentUser.UserId;
            if (firebase.DbReference != null)
            {
                firebase.DbReference.Child("players").Child(uid).Child("clams").SetValueAsync(0);
            }
        }
        else
        {
            if (firebase.DbReference != null)
            {
                firebase.DbReference.Child("players").Child("player1").Child("clams").SetValueAsync(0);
            }
        }
#endif
    }

    void Update()
    {
        if (isDead) return;
        move = Input.GetAxisRaw("Horizontal");
        if (move == 0f && MobileInput.Instance != null) { move = MobileInput.Instance.horizontal; }
        if (rb2D != null) rb2D.linearVelocity = new Vector2(move * speed, rb2D.linearVelocity.y);
        if (move != 0) transform.localScale = new Vector3(Mathf.Sign(move), 1, 1);

        if (Input.GetButtonDown("Jump")) TryJump();
        if (MobileInput.Instance != null && MobileInput.Instance.jump) { TryJump(); MobileInput.Instance.jump = false; }

        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(move));
            animator.SetFloat("VerticalVelocity", rb2D != null ? rb2D.linearVelocity.y : 0f);
            animator.SetBool("IsGrounded", isGrounded);
        }
    }

    private void TryJump()
    {
        if (jumpsLeft <= 0) return;
        float currentJumpForce = isGrounded ? jumpForce : doubleJumpForce;
        if (rb2D != null) rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, currentJumpForce);
        jumpsLeft--;
    }

    void FixedUpdate()
    {
        if (isDead) return;
        if (groundCheck != null)
        {
            bool wasGrounded = isGrounded;
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
            if (!wasGrounded && isGrounded) jumpsLeft = maxJumps;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;
        var fi = GetFirebase();

        // COIN
        if (collision.transform.CompareTag("Coin"))
        {
            AudioManager.Instance.PlayCoinSound();
            int value = 1;
            Coin coin = collision.GetComponent<Coin>();
            if (coin != null) value = coin.value;
            coins += value;
            if (textCoins != null) textCoins.text = coins.ToString();
            int totalClams = coins + specialClams;

            // PROTECCIÓN WEBGL
#if !UNITY_WEBGL
            if (fi != null)
            {
                if (fi.CurrentUser != null) { fi.SaveClams(totalClams); }
                else
                {
                    if (fi.DbReference != null)
                    {
                        fi.DbReference.Child("players").Child("player1").Child("clams").SetValueAsync(totalClams);
                    }
                }
            }
#endif

            Destroy(collision.gameObject);
            return;
        }

        // SPECIALCLAM
        if (collision.transform.CompareTag("SpecialClam"))
        {
            AudioManager.Instance.PlayCoinSound();
            specialClams++;
            if (textSpecialClam != null) textSpecialClam.text = specialClams.ToString();
            if (onSpecialClamCollected != null) onSpecialClamCollected.Invoke();
            int totalClams = coins + specialClams;

            // PROTECCIÓN WEBGL
#if !UNITY_WEBGL
            if (fi != null)
            {
                if (fi.CurrentUser != null) { fi.SaveClams(totalClams); }
                else
                {
                    if (fi.DbReference != null)
                    {
                        fi.DbReference.Child("players").Child("player1").Child("clams").SetValueAsync(totalClams);
                    }
                }
            }
#endif

            Destroy(collision.gameObject);
            return;
        }

        // DAMAGE
        if (collision.transform.CompareTag("Damage"))
        {
            AudioManager.Instance.PlayDamageVoice();
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.ApplyDamage(this.gameObject, collision.transform.position, 5f);
            }
            else
            {
                Vector2 knockbackDir = (rb2D.position - (Vector2)collision.transform.position).normalized;
                rb2D.linearVelocity = Vector2.zero;
                rb2D.AddForce(knockbackDir * 5f, ForceMode2D.Impulse);
            }
            return;
        }

        // SPIKES
        if (collision.transform.CompareTag("Spikes"))
        {
            if (LevelManager.Instance != null) LevelManager.Instance.ApplySpikeDeath(this.gameObject);
            else
            {
                isDead = true;
                AudioManager.Instance.PlayFallDeathVoice();

                if (rb2D != null) { rb2D.linearVelocity = Vector2.zero; rb2D.simulated = false; }
                Collider2D[] playerCols = GetComponents<Collider2D>();
                foreach (Collider2D c in playerCols) { c.enabled = false; }
                if (animator != null) { animator.ResetTrigger("Death"); animator.Play("death", 0, 0f); }
                this.enabled = false;
                StartCoroutine(RestartAfterDeath());
            }
            return;
        }

        // BARREL
        if (collision.transform.CompareTag("Barrel"))
        {
            AudioManager.Instance.PlayBarrelBreak();
            Vector2 knockbackDir = (rb2D.position - (Vector2)collision.transform.position).normalized;
            if (rb2D != null) rb2D.linearVelocity = Vector2.zero;
            rb2D.AddForce(knockbackDir * 6, ForceMode2D.Impulse);
            BoxCollider2D[] colliders = collision.gameObject.GetComponents<BoxCollider2D>();
            foreach (BoxCollider2D col in colliders) { col.enabled = false; }
            Animator barrelAnim = collision.GetComponent<Animator>();
            if (barrelAnim != null) barrelAnim.enabled = true;
            Destroy(collision.gameObject, 0.5f);
        }
    }

    public void ForceDie()
    {
        if (isDead) return;
        isDead = true;
        AudioManager.Instance.PlayLivesDeathVoice();

        if (rb2D != null) rb2D.linearVelocity = Vector2.zero;
        rb2D.simulated = false;
        Collider2D[] playerCols = GetComponents<Collider2D>();
        foreach (Collider2D c in playerCols) { c.enabled = false; }
        if (animator != null) { animator.ResetTrigger("Death"); animator.Play("death", 0, 0f); }
        this.enabled = false;
        StartCoroutine(RestartAfterDeath());
    }

    private IEnumerator RestartAfterDeath()
    {
        yield return new WaitForSeconds(1.2f);
        if (QuestionManager.Instance != null)
        {
            QuestionManager.Instance.ResetSessionAsked(false);
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}