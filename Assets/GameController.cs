using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    [Header("Progress")]
    public Slider progressSlider;
    private int progressAmount = 0;

    [Header("Player")]
    public GameObject player;

    [Header("Level Spawns")]
    public List<Transform> playerSpawns;

    [Header("Levels")]
    public List<GameObject> levelParents;
    public List<Tilemap> levelTilemaps;

    [Header("Spawner")]
    public ObjectSpawner objectSpawner;

    private int currentLevelIndex = 0;

    [Header("UI")]
    public GameObject loadCanvas;
    public Image holdFillCircle;
    private HoldToProgress holdToProgressScript;

    private void Awake()
    {
        // Subscribe to events
        Coin.OnCoinCollect += IncreaseProgressAmount;
        HoldToProgress.OnHoldComplete += LoadNextLevel;

        // Hide load canvas initially
        if (loadCanvas != null)
        {
            loadCanvas.SetActive(false);
            holdToProgressScript = loadCanvas.GetComponent<HoldToProgress>();
            if (holdToProgressScript != null)
                holdToProgressScript.fillCircle = holdFillCircle;
        }

        // Deactivate all levels at start
        for (int i = 0; i < levelParents.Count; i++)
            levelParents[i].SetActive(false);

        // Always reset to first level when scene loads
        ResetToFirstLevel();
    }

    public void ResetToFirstLevel()
    {
        currentLevelIndex = 0;

        // Activate first level only
        for (int i = 0; i < levelParents.Count; i++)
            levelParents[i].SetActive(i == currentLevelIndex);

        // Reset progress
        progressAmount = 0;
        if (progressSlider != null)
            progressSlider.value = 0;

        // Place player at first spawn
        if (player != null && playerSpawns.Count > 0 && playerSpawns[0] != null)
        {
            player.SetActive(true);
            player.transform.position = playerSpawns[0].position;
        }

        // Initialize ObjectSpawner
        if (objectSpawner != null && levelTilemaps.Count > 0)
            objectSpawner.SetTilemap(levelTilemaps[0]);

        // Reset Hold UI
        if (holdToProgressScript != null)
            holdToProgressScript.ResetHold();
    }

    private void IncreaseProgressAmount(int amount)
    {
        progressAmount += amount;
        if (progressSlider != null)
            progressSlider.value = progressAmount;

        if (progressAmount >= 100 && loadCanvas != null)
            loadCanvas.SetActive(true);
    }

    private void LoadNextLevel()
    {
        if (loadCanvas != null)
            loadCanvas.SetActive(false);

        // Deactivate current level
        if (levelParents.Count > currentLevelIndex)
            levelParents[currentLevelIndex].SetActive(false);

        // Increment level
        currentLevelIndex = (currentLevelIndex + 1) % levelParents.Count;

        // Activate next level
        if (levelParents.Count > currentLevelIndex)
            levelParents[currentLevelIndex].SetActive(true);

        // Move player
        if (player != null && playerSpawns.Count > currentLevelIndex && playerSpawns[currentLevelIndex] != null)
            player.transform.position = playerSpawns[currentLevelIndex].position;

        // Reset progress
        progressAmount = 0;
        if (progressSlider != null)
            progressSlider.value = progressAmount;

        // Update ObjectSpawner
        if (objectSpawner != null && levelTilemaps.Count > currentLevelIndex)
            objectSpawner.SetTilemap(levelTilemaps[currentLevelIndex]);

        // Reset Hold UI
        if (holdToProgressScript != null)
            holdToProgressScript.ResetHold();
    }

    private void OnDestroy()
    {
        Coin.OnCoinCollect -= IncreaseProgressAmount;
        HoldToProgress.OnHoldComplete -= LoadNextLevel;
    }
}
