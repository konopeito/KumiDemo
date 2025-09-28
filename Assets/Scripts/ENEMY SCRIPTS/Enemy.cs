using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Enemy : MonoBehaviour
{
    [Header("References")]
    public Transform player;                 // Reference to the player transform
    public Transform groundCheck;            // Position to check if grounded
    public Animator animator;                // Animator for handling enemy animations
    public PlayerHealthUI playerHealthUI;    // Reference to the player's health UI

    [Header("Stats")]
    public int maxHealth = 50;               // Maximum health for the enemy
    public int currentHealth;                // Current health for the enemy

    [Header("Movement")]
    public float patrolSpeed = 1.5f;         // Speed while patrolling
    public float chaseSpeed = 3f;            // Speed while chasing the player
    public float jumpForce = 8f;             // Force applied when enemy jumps
    public float chaseRange = 8f;            // Range at which enemy starts chasing the player

    [Header("Ground Check")]
    public float groundCheckRadius = 0.12f;  // Radius for ground detection
    public LayerMask groundLayer;            // Layer mask for ground detection

    [Header("Optional Player Jump")]
    public bool jumpTowardsPlayer = true;    // If true, enemy may jump towards the player
    public float extraJumpMultiplier = 1.2f; // Multiplier for jump height towards player

    [Header("Patrol Settings")]
    public float pauseTime = 0.5f;           // Pause duration before flipping direction
    private bool isPaused = false;           // Flag to check if enemy is paused during patrol
    private float jumpCooldown = 1f;         // Minimum time between consecutive jumps
    private float lastJumpTime = 0f;         // Last time the enemy jumped

    [Header("Damage Settings")]
    public int damageAmount = 10;            // Damage dealt to the player
    public float damageCooldown = 1f;        // Time between consecutive player damage
    private float lastDamageTime;            // Last time damage was applied

    [Header("Pushback Settings")]
    public float pushBackForceX = 1f;        // Horizontal pushback applied to the player
    public float pushBackForceY = 1.5f;      // Vertical pushback applied to the player

    private Rigidbody2D rb;                  // Rigidbody2D for physics control
    private SpriteRenderer sr;               // SpriteRenderer to flip the enemy sprite
    private float moveDirection = 1f;        // Direction of movement (1 = right, -1 = left)
    private bool isDead = false;             // Flag to track if enemy is dead

    // --- Initialization ---
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        currentHealth = maxHealth;

        if (groundCheck == null) groundCheck = transform;
        if (animator == null) Debug.LogWarning("[Enemy] Animator not assigned!");
        if (player == null) Debug.LogWarning("[Enemy] Player Transform not assigned!");
        if (playerHealthUI == null) Debug.LogWarning("[Enemy] PlayerHealthUI not assigned!");
    }

    // --- Called every frame to handle movement, chasing, and animations ---
    void Update()
    {
        if (isDead || player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool isChasing = distanceToPlayer <= chaseRange;

        if (!isPaused)
        {
            // Decide movement direction based on patrol or chase
            if (isChasing)
            {
                moveDirection = Mathf.Sign(player.position.x - transform.position.x);
            }
            else
            {
                PatrolLogic();
            }
        }

        // Flip the sprite based on velocity direction
        if (sr != null)
            sr.flipX = rb.velocity.x < 0;

        // Update animator with movement state
        if (animator != null)
            animator.SetBool("isMoving", Mathf.Abs(rb.velocity.x) > 0.1f);
    }

    // --- Called every fixed frame for physics-related updates ---
    void FixedUpdate()
    {
        if (isDead || player == null) return;

        float speed = Vector2.Distance(transform.position, player.position) <= chaseRange ? chaseSpeed : patrolSpeed;
        rb.velocity = new Vector2(moveDirection * speed, rb.velocity.y);

        // Check if enemy should jump (grounded + cooldown)
        if (IsGrounded() && Time.time - lastJumpTime > jumpCooldown)
        {
            JumpIfNeeded();
        }
    }

    // --- Patrol behavior ---
    void PatrolLogic()
    {
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.05f;
        RaycastHit2D wallAhead = Physics2D.Raycast(origin, Vector2.right * moveDirection, 0.6f, groundLayer);
        RaycastHit2D groundAhead = Physics2D.Raycast(origin + Vector2.right * 0.5f * moveDirection, Vector2.down, 0.9f, groundLayer);

        if (wallAhead.collider || !groundAhead.collider)
        {
            StartCoroutine(PauseAndFlip());
        }
    }

    // --- Pause movement and flip direction for patrol ---
    IEnumerator PauseAndFlip()
    {
        isPaused = true;
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(pauseTime);
        moveDirection *= -1f;
        isPaused = false;
    }

    // --- Determine if enemy should jump (towards player or to avoid obstacle) ---
    void JumpIfNeeded()
    {
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.1f;

        RaycastHit2D wallAhead = Physics2D.Raycast(origin, Vector2.right * moveDirection, 0.6f, groundLayer);
        RaycastHit2D groundAhead = Physics2D.Raycast(origin + Vector2.right * 0.5f * moveDirection, Vector2.down, 1f, groundLayer);

        bool jumpForPlayer = jumpTowardsPlayer && player.position.y > transform.position.y + 1f;

        if (wallAhead.collider || !groundAhead.collider || jumpForPlayer)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0f);
            float appliedJump = jumpForce * (jumpForPlayer ? extraJumpMultiplier : 1f);
            rb.AddForce(Vector2.up * appliedJump, ForceMode2D.Impulse);
            lastJumpTime = Time.time; // Update cooldown
        }
    }

    // --- Checks if enemy is touching the ground ---
    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    // --- Handle collision with player for damage and pushback ---
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead || playerHealthUI == null) return;

        if (collision.collider.CompareTag("Player") && Time.time - lastDamageTime > damageCooldown)
        {
            // Deal damage to player
            playerHealthUI.TakeDamage(damageAmount);
            lastDamageTime = Time.time;

            Rigidbody2D playerRb = collision.collider.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                float verticalDiff = player.position.y - transform.position.y;

                // Apply pushback depending on relative positions
                if (verticalDiff < 0) // Enemy on top of player
                {
                    rb.velocity = new Vector2(moveDirection * pushBackForceX, pushBackForceY);
                }
                else if (verticalDiff > 0) // Player on top of enemy
                {
                    playerRb.velocity = new Vector2(playerRb.velocity.x, pushBackForceY);
                }
            }
        }
    }

    // --- Apply damage to enemy with knockback and animation ---
    public void TakeDamage(int damage, Vector2 knockbackDir)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        // Log current health for debugging
        Debug.Log($"{name} took {damage} damage. Current HP: {currentHealth}");

        // Trigger hurt animation
        if (animator != null)
            animator.SetTrigger("Hurt");

        // Apply knockback
        rb.velocity = Vector2.zero;
        rb.AddForce(knockbackDir, ForceMode2D.Impulse);

        if (currentHealth <= 0)
            Die();
    }

    // --- Handle enemy death ---
    public void Die()
    {
        if (isDead) return;
        isDead = true;

        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (animator != null)
            animator.SetTrigger("Die");

        Destroy(gameObject, 1f); // Delay for death animation
    }

    // --- Draw debug gizmos for ground check and chase range ---
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
        }
    }
}
