using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections; // Needed for retry/reload

public class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    public Slider healthSlider;          // Reference to the HP slider (health bar)
    public Image heartImage;             // Reference to the heart image for health visualization
    public Animator heartAnimator;       // Animator that plays feedback animations (hurt/heal)
    public GameObject gameOverPanel;     // Reference to the Game Over UI panel
    public PlayerMovement playerMovement; // Reference to player movement script

    [Header("Heart Sprites")]
    public Sprite fullHeart;             // Full heart sprite
    public Sprite threeQuarterHeart;     // 75% heart sprite
    public Sprite halfHeart;             // 50% heart sprite
    public Sprite quarterHeart;          // 25% heart sprite
    // No emptyHeart sprite needed anymore
    [Header("Key Visual")]
    public Transform keyTransform; // Assign the key GameObject here
    public Vector3 keyOffset = new Vector3(0, 1f, 0); // Position above player

    [Header("Player Health Settings")]
    public int maxHealth = 100;          // Maximum health value
    public int currentHealth;            // Current health value

    private bool hasDied = false;        // Prevent multiple death triggers

    private void Awake()
    {
        currentHealth = maxHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        UpdateHeartUI();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false); // Hide Game Over panel at start
    }

    private void Start()
    {
        Debug.Log("PlayerHealthUI initialized. Current HP: " + currentHealth);
    }

    private void Update()
    {
        // Debug key: instantly kill player with K
        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log("Debug Key pressed (K) → Forcing Game Over.");
            currentHealth = 0;
            UpdateUI();
            TriggerDeath();
        }

        // Regular death check
        if (currentHealth <= 0 && !hasDied)
        {
            TriggerDeath();
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        Debug.Log("Player took damage: -" + amount + " | Current HP: " + currentHealth);

        UpdateUI();

        if (heartAnimator != null)
            heartAnimator.SetTrigger("Hurt");

        if (currentHealth <= 0 && !hasDied)
        {
            TriggerDeath();
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        Debug.Log("Player healed: +" + amount + " | Current HP: " + currentHealth);

        UpdateUI();

        if (heartAnimator != null)
            heartAnimator.SetTrigger("Heal");
    }

    private void UpdateUI()
    {
        if (healthSlider != null)
            healthSlider.value = currentHealth;

        UpdateHeartUI();
    }

    private void UpdateHeartUI()
    {
        if (heartImage == null) return;

        float healthPercent = (float)currentHealth / maxHealth;

        if (healthPercent >= 0.75f) { heartImage.sprite = fullHeart; heartImage.gameObject.SetActive(true); }
        else if (healthPercent >= 0.5f) { heartImage.sprite = threeQuarterHeart; heartImage.gameObject.SetActive(true); }
        else if (healthPercent >= 0.25f) { heartImage.sprite = halfHeart; heartImage.gameObject.SetActive(true); }
        else if (healthPercent > 0f) { heartImage.sprite = quarterHeart; heartImage.gameObject.SetActive(true); }
        else { heartImage.gameObject.SetActive(false); } // Hide when dead
    }

    // Triggers player death animation and Game Over panel
    private void TriggerDeath()
    {
        if (hasDied) return;
        hasDied = true;

        // Trigger player sprite death animation
        if (playerMovement != null)
            playerMovement.Die();

        // Start coroutine to show Game Over panel after death animation
        StartCoroutine(ShowGameOverAfterDelay());
    }

    private IEnumerator ShowGameOverAfterDelay()
    {
        // Optional: adjust this to match your death animation length
        float deathAnimationDuration = 1.5f;

        yield return new WaitForSecondsRealtime(deathAnimationDuration);

        // Activate Game Over panel
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        else
            Debug.LogError("Game Over Panel not assigned in Inspector!");

        // Freeze the game after showing Game Over
        Time.timeScale = 0f;

        Debug.Log("Player has died. Game Over triggered.");
    }

    // Reloads the current scene (Retry button)
    public void Retry()
    {
        Time.timeScale = 1f; // Unfreeze game
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Quits the game (Quit button)
    public void QuitGame()
    {
        Debug.Log("Quit Game triggered.");
        Application.Quit();
    }
}
