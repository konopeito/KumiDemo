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

    [Header("Movement")]
    public float moveSpeed = 5f;
    float horizontalMovement;
    bool isFacingRight = true;

    [Header("Jumping")]
    public float jumpPower = 8f;
    public int maxJumps = 2;
    int jumpsRemaining;

    [Header("Gravity")]
    public float baseGravity = 2f;
    public float maxFallSpeed = 25f;
    public float fallSpeedMultiplier = 2f;

    [Header("Ground Check")]
    public Transform groundCheckPos;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    public LayerMask groundLayer;
    bool isGrounded;

    [Header("Wall Check")]
    public Transform wallCheckPos;
    public Vector2 wallCheckSize = new Vector2(0.1f, 1f);
    public LayerMask wallLayer;

    [Header("Wall Movement")]
    public float wallSlideSpeed = 2f;
    bool isWallSliding;
    bool isWallJumping;
    float wallJumpDirection;
    public Vector2 wallJumpPower = new Vector2(5f, 8f);
    public float wallJumpTime = 0.5f;

    [Header("Idle & Animations")]
    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int jumpHash = Animator.StringToHash("Jump");
    private readonly int sit1Hash = Animator.StringToHash("Sit_1");
    private readonly int sit2Hash = Animator.StringToHash("Sit_2");
    private readonly int attack1Hash = Animator.StringToHash("Attack_1");
    private readonly int attack2Hash = Animator.StringToHash("Attack_2");
    private readonly int idleSwitchHash = Animator.StringToHash("IdleSwitch");

    private float idleTimer = 0f;
    public float idleSwitchInterval = 3f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        GroundCheck();
        ProcessGravity();
        HandleWallSlide();

        // Horizontal movement only if not wall jumping
        if (!isWallJumping)
        {
            rb.velocity = new Vector2(horizontalMovement * moveSpeed, rb.velocity.y);
            Flip();
        }

        // Animate movement
        animator.SetFloat(speedHash, Mathf.Abs(horizontalMovement));

        // Idle alternation
        if (Mathf.Abs(horizontalMovement) < 0.1f)
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
    }

    public void Move(InputAction.CallbackContext context)
    {
        horizontalMovement = context.ReadValue<Vector2>().x;
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (isWallSliding && Mathf.Sign(horizontalMovement) == -wallJumpDirection)
            {
                // Wall jump
                isWallJumping = true;
                rb.velocity = new Vector2(wallJumpDirection * wallJumpPower.x, wallJumpPower.y);
                JumpFX();

                // Flip character away from wall
                Vector3 ls = transform.localScale;
                ls.x = wallJumpDirection;
                transform.localScale = ls;
                isFacingRight = ls.x > 0;

                // Reset wall jump after a short time
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
            // Light tap jump
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
            jumpsRemaining--;
            JumpFX();
        }
    }

    private void JumpFX()
    {
        dustFX.Play();
    }
    private void CancelWallJump()
    {
        isWallJumping = false;
    }

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

    private bool IsTouchingWall()
    {
        return Physics2D.OverlapBox(wallCheckPos.position, wallCheckSize, 0f, wallLayer);
    }

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

    private void Flip()
    {
        if (isFacingRight && horizontalMovement < 0 || !isFacingRight && horizontalMovement > 0)
        {
            isFacingRight = !isFacingRight;
            Vector3 ls = transform.localScale;
            ls.x *= -1f;
            transform.localScale = ls;
            if(rb.velocity.y == 0)
            {
                dustFX.Play();
            }
        }
    }

    void HandleActionInput()
    {
        if (Keyboard.current.zKey.wasPressedThisFrame)
            animator.SetTrigger(sit1Hash);
        if (Keyboard.current.xKey.wasPressedThisFrame)
            animator.SetTrigger(sit2Hash);
        if (Keyboard.current.cKey.wasPressedThisFrame)
            animator.SetTrigger(attack1Hash);
        if (Keyboard.current.vKey.wasPressedThisFrame)
            animator.SetTrigger(attack2Hash);
    }

    void LateUpdate()
    {
        HandleActionInput();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(groundCheckPos.position, groundCheckSize);

        Gizmos.color = Color.green;
        Gizmos.DrawCube(wallCheckPos.position, wallCheckSize);
    }
}
