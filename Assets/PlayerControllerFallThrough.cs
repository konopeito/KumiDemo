using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerControllerFallThrough : MonoBehaviour
{
    [Header("Fall Through Settings")]
    public LayerMask platformLayer; // Assign your Platform/Tilemap layer here
    public float fallThroughDuration = 0.5f; // Time player falls through platform

    private Collider2D playerCollider;

    void Awake()
    {
        playerCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        // Detect if the player presses Down + Jump/Down Arrow
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            // Check if standing on a platform
            if (IsStandingOnPlatform())
            {
                StartCoroutine(FallThroughPlatform());
            }
        }
    }

    /// <summary>
    /// Checks if the player is currently standing on a platform layer.
    /// </summary>
    private bool IsStandingOnPlatform()
    {
        // Cast a small box downwards to detect platform
        RaycastHit2D hit = Physics2D.BoxCast(playerCollider.bounds.center, playerCollider.bounds.size, 0f, Vector2.down, 0.1f, platformLayer);
        return hit.collider != null;
    }

    /// <summary>
    /// Temporarily disables collision with platforms to allow fall-through.
    /// </summary>
    private IEnumerator FallThroughPlatform()
    {
        // Ignore collision with platform layer
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMaskToLayer(platformLayer), true);

        // Wait for fall-through duration
        yield return new WaitForSeconds(fallThroughDuration);

        // Re-enable collision
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMaskToLayer(platformLayer), false);
    }

    /// <summary>
    /// Converts a LayerMask with a single layer to a layer index.
    /// </summary>
    private int LayerMaskToLayer(LayerMask mask)
    {
        int layer = 0;
        int value = mask.value;
        while (value > 1)
        {
            value = value >> 1;
            layer++;
        }
        return layer;
    }
}
