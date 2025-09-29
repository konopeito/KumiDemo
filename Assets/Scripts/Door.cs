using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("UI")]
    public GameObject levelClearUI; // Canvas to show level cleared menu

    // --- Detects player interaction ---
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerMovement player = collision.GetComponent<PlayerMovement>();
            if (player != null && player.hasKey)
            {
                // Example: Press E to interact with door
                if (Input.GetKeyDown(KeyCode.E))
                {
                    LevelClear(player);
                }
            }
        }
    }

    // --- Handles level clear logic ---
    private void LevelClear(PlayerMovement player)
    {
        // Show the UI
        if (levelClearUI != null)
        {
            levelClearUI.SetActive(true);
        }

        // Optional: Stop player movement
        player.isDead = true;

        // Optional: Play level complete sound or animation
        Debug.Log("Level Cleared!");
    }
}
