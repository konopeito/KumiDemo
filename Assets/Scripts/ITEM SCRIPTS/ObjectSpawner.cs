using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ObjectSpawner : MonoBehaviour
{
    public Tilemap tilemap; // Tilemap reference for finding valid ground tiles
    public GameObject[] objectPrefabs; // Array of prefabs (must match ObjectType enum order)

    [Header("Spawn Probabilities")] // Inspector tuning for random chance
    public float greenGemProbability = 0.2f;
    public float redGemProbability = 0.1f;
    public float blueGemProbability = 0.08f;
    public float blackPearlProbability = 0.03f;
    public float goldenGemProbability = 0.01f;
    public float enemyProbability = 0.1f;

    [Header("Spawn Settings")]
    public int maxObjects = 5;       // Maximum number of objects allowed at once
    public float gemLifeTime = 10f;  // How long gems last before disappearing
    public float spawnInterval = 2f; // Time between spawns
    public float randomOffsetX = 0.3f; // Small random horizontal offset so gems don’t stack
    public float verticalOffset = 0.9f; // Adjustable offset to lift objects above ground

    private List<Vector3> validSpawnPositions = new List<Vector3>(); // Cached valid positions on tilemap
    private List<GameObject> spawnObjects = new List<GameObject>();  // Track spawned objects
    private bool isSpawning = false; // Prevent multiple spawn coroutines running

    // --- Unity lifecycle ---
    private void Start()
    {
        GatherValidPositions(); // Collect all possible spawn positions
        StartCoroutine(SpawnObjectsIfNeeded()); // Start spawning loop
    }

    private void Update()
    {
        if (!tilemap.gameObject.activeInHierarchy)
        {
            //Level change
            LevelChange();
        }
        // If below max count and not already spawning, restart coroutine
        if (!isSpawning && ActiveObjectsCount() < maxObjects)
        {
            StartCoroutine(SpawnObjectsIfNeeded());
        }
    }

    private void LevelChange()
    {
        tilemap = GameObject.Find("Ground").GetComponent<Tilemap>();
        GatherValidPositions();
        DestroyAllSpawnedObjects();
    }

    // --- Helpers ---
    private int ActiveObjectsCount()
    {
        CleanSpawnObjects(); // Remove destroyed objects
        return spawnObjects.Count;
    }

    private void CleanSpawnObjects()
    {
        spawnObjects.RemoveAll(obj => obj == null);
    }

    private IEnumerator SpawnObjectsIfNeeded()
    {
        isSpawning = true;
        // Keep spawning until we reach maxObjects
        while (ActiveObjectsCount() < maxObjects)
        {
            SpawnObject();
            yield return new WaitForSeconds(spawnInterval); // Wait before next spawn
        }
        isSpawning = false;
    }

    private bool PositionHasObject(Vector3 positionToCheck)
    {
        CleanSpawnObjects(); // Ensure no destroyed objects are in the list
        // Prevent overlap: checks if an object already exists nearby
        return spawnObjects.Any(checkObj =>
            checkObj && Vector3.Distance(checkObj.transform.position, positionToCheck) < 0.5f);
    }

    private ObjectType RandomObjectType()
    {
        // Weighted random selection based on probabilities
        float totalProb = enemyProbability + redGemProbability + greenGemProbability +
                          blueGemProbability + blackPearlProbability + goldenGemProbability;

        float randomChoice = Random.value * totalProb;
        float cumulative = 0f;

        cumulative += enemyProbability;
        if (randomChoice <= cumulative) return ObjectType.Enemy;

        cumulative += redGemProbability;
        if (randomChoice <= cumulative) return ObjectType.RedGem;

        cumulative += greenGemProbability;
        if (randomChoice <= cumulative) return ObjectType.GreenGem;

        cumulative += blueGemProbability;
        if (randomChoice <= cumulative) return ObjectType.BlueGem;

        cumulative += blackPearlProbability;
        if (randomChoice <= cumulative) return ObjectType.BlackPearl;

        // If none matched, default to golden gem
        return ObjectType.GoldenGem;
    }

    private void SpawnObject()
    {
        if (validSpawnPositions.Count == 0) return;

        Vector3 spawnPosition = Vector3.zero;
        bool validPositionFound = false;

        // Try to find a valid position that’s not blocked
        while (!validPositionFound && validSpawnPositions.Count > 0)
        {
            int randomIndex = Random.Range(0, validSpawnPositions.Count);
            Vector3 potentialPosition = validSpawnPositions[randomIndex];

            // Check left/right spaces for overlap
            Vector3 leftPosition = potentialPosition + Vector3.left * 0.5f;
            Vector3 rightPosition = potentialPosition + Vector3.right * 0.5f;

            if (!PositionHasObject(leftPosition) && !PositionHasObject(rightPosition))
            {
                // Apply slight random horizontal offset
                spawnPosition = potentialPosition + new Vector3(
                    Random.Range(-randomOffsetX, randomOffsetX), 0f, 0f);
                validPositionFound = true;
            }

            // Remove tested position to avoid repeats
            validSpawnPositions.RemoveAt(randomIndex);
        }

        if (validPositionFound)
        {
            // Choose type and instantiate object
            ObjectType objectType = RandomObjectType();

            // Ensure prefab exists
            if (objectPrefabs.Length > (int)objectType && objectPrefabs[(int)objectType] != null)
            {
                GameObject spawned = Instantiate(
                    objectPrefabs[(int)objectType], spawnPosition, Quaternion.identity);

                spawnObjects.Add(spawned);

                // If it’s not an enemy, destroy after lifetime
                if (objectType != ObjectType.Enemy)
                {
                    StartCoroutine(DestroyObjectAfterTime(spawned, gemLifeTime));
                }
            }
            else
            {
                Debug.LogWarning($"Prefab for {objectType} is missing in objectPrefabs array!");
            }
        }
    }

    private IEnumerator DestroyObjectAfterTime(GameObject obj, float time)
    {
        // Wait for set lifetime then remove
        yield return new WaitForSeconds(time);
        if (obj != null)
        {
            spawnObjects.Remove(obj);
            validSpawnPositions.Add(obj.transform.position); // Free up position again
            Destroy(obj);
        }
    }

    private void DestroyAllSpawnedObjects()
    {
        foreach (GameObject obj in spawnObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnObjects.Clear();
    }

    private void GatherValidPositions()
    {
        // Collect tilemap cells where objects can spawn
        validSpawnPositions.Clear();
        BoundsInt bounds = tilemap.cellBounds;
        TileBase[] allTiles = tilemap.GetTilesBlock(bounds);

        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                TileBase tile = allTiles[x + y * bounds.size.x];

                if (tile != null)
                {
                    // Check if the tile ABOVE is empty (so object sits on this one)
                    int aboveIndex = x + (y + 1) * bounds.size.x;
                    if (aboveIndex < allTiles.Length && allTiles[aboveIndex] == null)
                    {
                        // Convert grid position to world position
                        Vector3Int tilePos = new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0);
                        Vector3 pos = tilemap.GetCellCenterWorld(tilePos);

                        // Apply offset so object sits nicely above tile
                        pos.y += verticalOffset;

                        validSpawnPositions.Add(pos);
                    }
                }
            }
        }
    }

    // --- Scene view visualization ---
    private void OnDrawGizmosSelected()
    {
        // Draw cyan circles at valid spawn points for debugging
        Gizmos.color = Color.cyan;
        foreach (Vector3 pos in validSpawnPositions)
        {
            Gizmos.DrawWireSphere(pos, 0.1f);
        }
    }
}

// Enum maps directly to objectPrefabs order
public enum ObjectType
{
    GreenGem = 0,
    RedGem = 1,
    BlueGem = 2,
    BlackPearl = 3,
    GoldenGem = 4,
    Enemy = 5
}

