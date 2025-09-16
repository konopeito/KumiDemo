using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public Rigidbody2D rb;

    [Header("Movement")]
    public float moveSpeed = 5f;
    float horizontalMovement;

    [Header("Jumping")]
    public float jumpPower = 10f;

    [Header("GroundCheck")]
    public Transform groundCheckPos;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.05f);
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("Animation")]
    public Animator animator;

    // Animator hashes for triggers / parameters
    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int jumpHash = Animator.StringToHash("Jump");
    private readonly int sit1Hash = Animator.StringToHash("Sit_1");
    private readonly int sit2Hash = Animator.StringToHash("Sit_2");
    private readonly int attack1Hash = Animator.StringToHash("Attack_1");
    private readonly int attack2Hash = Animator.StringToHash("Attack_2");
    private readonly int idleSwitchHash = Animator.StringToHash("IdleSwitch"); // NEW: Idle alternation trigger

    // Idle switch timer
    private float idleTimer = 0f;
    public float idleSwitchInterval = 3f; // seconds between alternating Idle_1 / Idle_2

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Movement
        rb.velocity = new Vector2(horizontalMovement * moveSpeed, rb.velocity.y);

        // Animate horizontal movement
        animator.SetFloat(speedHash, Mathf.Abs(horizontalMovement));

        // Flip sprite based on movement direction
        if (horizontalMovement > 0.1f) transform.localScale = new Vector3(1, 1, 1);
        else if (horizontalMovement < -0.1f) transform.localScale = new Vector3(-1, 1, 1);

        // Ground check
        isGrounded = Physics2D.OverlapBox(groundCheckPos.position, groundCheckSize, 0f, groundLayer);

        // Idle alternation logic
        if (Mathf.Abs(horizontalMovement) < 0.1f) // player is standing still
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
            idleTimer = 0f; // reset timer if moving
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        horizontalMovement = context.ReadValue<Vector2>().x;
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpPower);
            animator.SetTrigger(jumpHash); // Trigger jump animation
        }
        else if (context.canceled)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
        }
    }

    void HandleActionInput()
    {
        // Sit
        if (Keyboard.current.zKey.wasPressedThisFrame)
            animator.SetTrigger(sit1Hash);
        if (Keyboard.current.xKey.wasPressedThisFrame)
            animator.SetTrigger(sit2Hash);

        // Attack
        if (Keyboard.current.cKey.wasPressedThisFrame)
            animator.SetTrigger(attack1Hash);
        if (Keyboard.current.vKey.wasPressedThisFrame)
            animator.SetTrigger(attack2Hash);

        // Add more keys/triggers for other actions as needed
    }

    void LateUpdate()
    {
        HandleActionInput();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(groundCheckPos.position, groundCheckSize);
    }
}
