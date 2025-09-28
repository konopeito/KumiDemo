using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Components")]
    public Rigidbody2D rb;
    public Animator animator;
    public ParticleSystem dustFX;

    [Header("Player State")]
    public bool isDead = false;
    private bool isHurt = false; // Tracks if the player is in hurt state

    [Header("Movement")]
    public float moveSpeed = 5f;
    private float horizontalMovement;
    private bool isFacingRight = true;

    [Header("Jumping")]
    public float jumpPower = 8f;
    public int maxJumps = 2;
    private int jumpsRemaining;

    [Header("Gravity")]
    public float baseGravity = 2f;
    public float maxFallSpeed = 25f;
    public float fallSpeedMultiplier = 2f;

    [Header("Ground Check")]
    public Transform groundCheckPos;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("Wall Check")]
    public Transform wallCheckPos;
    public Vector2 wallCheckSize = new Vector2(0.1f, 1f);
    public LayerMask wallLayer;

    [Header("Wall Movement")]
    public float wallSlideSpeed = 2f;
    private bool isWallSliding;
    private bool isWallJumping;
    private float wallJumpDirection;
    public Vector2 wallJumpPower = new Vector2(5f, 8f);
    public float wallJumpTime = 0.5f;

    [Header("Idle & Animations")]
    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int jumpHash = Animator.StringToHash("Jump");
    private readonly int sit1Hash = Animator.StringToHash("Sit_1");
    private readonly int sit2Hash = Animator.StringToHash("Sit_2");
    private readonly int attack1Hash = Animator.StringToHash("Attack_1");
    private readonly int attack2Hash = Animator.StringToHash("Attack_2");
    private readonly int attack3Hash = Animator.StringToHash("Attack_3");
    private readonly int attack4Hash = Animator.StringToHash("Attack_4");
    private readonly int idleSwitchHash = Animator.StringToHash("IdleSwitch");
    private readonly int hurtHash = Animator.StringToHash("Hurt"); // Player hurt animation
    private readonly int pickupHash = Animator.StringToHash("Pickup"); // Player pickup animation

    private float idleTimer = 0f;
    public float idleSwitchInterval = 3f;

    [Header("Attack Settings")]
    public float attackRange = 1f;
    public int attackDamage = 10;
    public Vector2 knockback = new Vector2(2f, 3f);
    public LayerMask enemyLayer;
    private bool isAttacking = false;
    public float attackCooldown = 0.5f;

    [Header("Key Item")]
    public bool hasKey = false; // Tracks if the player has picked up the key

    [Header("One-Way Platform Drop")]
    public LayerMask oneWayPlatformLayer; // Layer for one-way platforms
    public float fallThroughDuration = 0.2f; // Time player ignores platform collisions
    private Collider2D playerCollider;

    private void Awake()
    {
        playerCollider = GetComponent<Collider2D>();
    }

    // --- Called every frame: handles movement, jumping, wall interactions, and idle animations ---
    void Update()
    {
        if (isDead || isHurt) return; // Prevent movement when dead or hurt

        GroundCheck();
        ProcessGravity();
        HandleWallSlide();

        // Movement disabled while wall jumping or attacking
        if (!isWallJumping && !isAttacking)
        {
            rb.velocity = new Vector2(horizontalMovement * moveSpeed, rb.velocity.y);
            Flip();
        }

        // Update animator speed
        animator.SetFloat(speedHash, Mathf.Abs(horizontalMovement));

        // Handle idle animation switching when player is stationary
        if (!isAttacking && Mathf.Abs(horizontalMovement) < 0.1f)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleSwitchInterval)
            {
                animator.SetTrigger(idleSwitchHash);
                idleTimer = 0f;
            }
        }
        else
        {
            idleTimer = 0f;
        }

        // Check for fall-through input
        if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            StartCoroutine(FallThroughPlatform());
        }
    }

    // --- Handles player death ---
    public void Die()
    {
        if (isDead) return;
        isDead = true;

        rb.velocity = Vector2.zero;
        horizontalMovement = 0f;
        rb.gravityScale = 0f;

        if (animator != null)
            animator.SetTrigger("Die");
    }

    // --- Handles player taking damage from enemies ---
    public void TakeDamage(int damage, Vector2 knockbackDir)
    {
        if (isDead || isHurt) return;

        isHurt = true;

        // Trigger hurt animation
        if (animator != null)
            animator.SetTrigger(hurtHash);

        // Apply knockback
        rb.velocity = Vector2.zero;
        rb.AddForce(knockbackDir, ForceMode2D.Impulse);

        // Recover from hurt after short delay
        StartCoroutine(RecoverFromHurt(0.3f));
    }

    // --- Resets the hurt state after knockback duration ---
    private IEnumerator RecoverFromHurt(float duration)
    {
        yield return new WaitForSeconds(duration);
        isHurt = false;
    }

    // --- Handles movement input from the player ---
    public void Move(InputAction.CallbackContext context)
    {
        if (!isDead && !isAttacking && !isHurt)
            horizontalMovement = context.ReadValue<Vector2>().x;
    }

    // --- Handles jump input including wall jumps ---
    public void Jump(InputAction.CallbackContext context)
    {
        if (isDead || isAttacking || isHurt) return;

        if (context.performed)
        {
            if (isWallSliding && Mathf.Sign(horizontalMovement) == -wallJumpDirection)
            {
                // Perform wall jump
                isWallJumping = true;
                rb.velocity = new Vector2(wallJumpDirection * wallJumpPower.x, wallJumpPower.y);
                JumpFX();

                Vector3 ls = transform.localScale;
                ls.x = wallJumpDirection;
                transform.localScale = ls;
                isFacingRight = ls.x > 0;

                Invoke(nameof(CancelWallJump), wallJumpTime);
            }
            else if (jumpsRemaining > 0)
            {
                // Normal jump
                rb.velocity = new Vector2(rb.velocity.x, jumpPower);
                jumpsRemaining--;
                animator.SetTrigger(jumpHash);
                JumpFX();
            }
        }
        else if (context.canceled && rb.velocity.y > 0)
        {
            // Short hop if jump button released early
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
            jumpsRemaining--;
            JumpFX();
        }
    }

    // --- Plays dust particle effect when jumping ---
    private void JumpFX()
    {
        dustFX.Play();
    }

    // --- Cancels wall jump state ---
    private void CancelWallJump()
    {
        isWallJumping = false;
    }

    // --- Applies gravity and caps fall speed ---
    private void ProcessGravity()
    {
        if (rb.velocity.y < 0)
        {
            rb.gravityScale = baseGravity * fallSpeedMultiplier;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -maxFallSpeed));
        }
        else
        {
            rb.gravityScale = baseGravity;
        }
    }

    // --- Checks if the player is on the ground ---
    private void GroundCheck()
    {
        if (Physics2D.OverlapBox(groundCheckPos.position, groundCheckSize, 0f, groundLayer))
        {
            jumpsRemaining = maxJumps;
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    // --- Checks if the player is touching a wall ---
    private bool IsTouchingWall()
    {
        return Physics2D.OverlapBox(wallCheckPos.position, wallCheckSize, 0f, wallLayer);
    }

    // --- Handles wall sliding mechanics ---
    private void HandleWallSlide()
    {
        if (!isGrounded && IsTouchingWall() && horizontalMovement != 0)
        {
            isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -wallSlideSpeed));
            wallJumpDirection = horizontalMovement > 0 ? -1f : 1f;
        }
        else
        {
            isWallSliding = false;
        }
    }

    // --- Flips the player sprite based on movement direction ---
    private void Flip()
    {
        if (isFacingRight && horizontalMovement < 0 || !isFacingRight && horizontalMovement > 0)
        {
            isFacingRight = !isFacingRight;
            Vector3 ls = transform.localScale;
            ls.x *= -1f;
            transform.localScale = ls;

            if (rb.velocity.y == 0)
                dustFX.Play();
        }
    }

    // --- Handles attack input and triggers attack coroutines ---
    public void Attack1(InputAction.CallbackContext context) { if (context.performed) StartCoroutine(PerformAttack(attack1Hash)); }
    public void Attack2(InputAction.CallbackContext context) { if (context.performed) StartCoroutine(PerformAttack(attack2Hash)); }
    public void Attack3(InputAction.CallbackContext context) { if (context.performed) StartCoroutine(PerformAttack(attack3Hash)); }
    public void Attack4(InputAction.CallbackContext context) { if (context.performed) StartCoroutine(PerformAttack(attack4Hash)); }

    // --- Performs attack animation, detects enemies, and applies knockback ---
    private IEnumerator PerformAttack(int attackHash)
    {
        if (isAttacking || isHurt) yield break;

        isAttacking = true;
        animator.SetTrigger(attackHash);

        yield return new WaitForSeconds(0.1f); // Delay before hit detection

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);
        foreach (var enemy in hitEnemies)
        {
            if (enemy.TryGetComponent<Enemy>(out Enemy e))
            {
                Vector2 knockbackDir = new Vector2(isFacingRight ? knockback.x : -knockback.x, knockback.y);
                e.TakeDamage(attackDamage, knockbackDir);
            }
        }

        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }

    // --- Handles picking up a key item ---
    public void PickupKey(GameObject keyObject)
    {
        hasKey = true;

        // Attach key above player
        keyObject.transform.SetParent(transform);
        keyObject.transform.localPosition = new Vector3(0f, 1f, 0f); // Adjust height above player
        keyObject.GetComponent<Collider2D>().enabled = false; // Disable collision

        Debug.Log("Player picked up the key!");
    }

    // --- Temporarily disables collisions with one-way platforms to fall through ---
    private IEnumerator FallThroughPlatform()
    {
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMaskToLayer(oneWayPlatformLayer), true);
        yield return new WaitForSeconds(fallThroughDuration);
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMaskToLayer(oneWayPlatformLayer), false);
    }

    // --- Converts a LayerMask to a single layer index ---
    private int LayerMaskToLayer(LayerMask mask)
    {
        int layer = 0;
        int maskValue = mask.value;
        while (maskValue > 0)
        {
            if ((maskValue & 1) == 1) return layer;
            maskValue = maskValue >> 1;
            layer++;
        }
        return 0;
    }

    // --- Draws debug gizmos for ground, wall, and attack range ---
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(groundCheckPos.position, groundCheckSize);

        Gizmos.color = Color.green;
        Gizmos.DrawCube(wallCheckPos.position, wallCheckSize);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
