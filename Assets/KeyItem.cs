using UnityEngine;

public class KeyItem : MonoBehaviour
{
    [Header("Key Pickup Settings")]
    public Vector3 offsetAbovePlayer = new Vector3(0, 1.5f, 0); // Position relative to player
    private bool isPickedUp = false; // Tracks if key has been picked up

    // --- Detects when the player collides with the key ---
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isPickedUp && collision.CompareTag("Player"))
        {
            PlayerMovement player = collision.GetComponent<PlayerMovement>();
            if (player != null)
            {
                // Set that player has the key
                player.hasKey = true;

                // Parent the key to the player so it follows
                transform.SetParent(player.transform);

                // Position it above the player
                transform.localPosition = offsetAbovePlayer;

                isPickedUp = true;

                Debug.Log("Player has picked up the key!");
            }
        }
    }
}
