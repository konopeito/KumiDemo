using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ObjectSpawner : MonoBehaviour
{
    [Header("Tilemap")]
    public Tilemap tilemap;

    [Header("Prefabs")]
    public GameObject[] objectPrefabs;

    [Header("Spawn Probabilities")]
    public float greenGemProbability = 0.2f;
    public float redGemProbability = 0.1f;
    public float blueGemProbability = 0.08f;
    public float blackPearlProbability = 0.03f;
    public float goldenGemProbability = 0.01f;
    public float enemyProbability = 0.1f;

    [Header("Spawn Settings")]
    public int maxObjects = 5;
    public float gemLifeTime = 10f;
    public float spawnInterval = 2f;
    public float randomOffsetX = 0.3f;
    public float verticalOffset = 0.9f;

    private List<Vector3> validSpawnPositions = new List<Vector3>();
    private List<GameObject> spawnObjects = new List<GameObject>();

    private void Start()
    {
        if (tilemap != null)
        {
            GatherValidPositions();
            StartCoroutine(SpawnObjectsLoop());
        }
    }

    public void SetTilemap(Tilemap newTilemap)
    {
        tilemap = newTilemap;
        GatherValidPositions();
        DestroyAllSpawnedObjects();
    }

    private IEnumerator SpawnObjectsLoop()
    {
        while (true)
        {
            if (ActiveObjectsCount() < maxObjects)
                SpawnObject();

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private int ActiveObjectsCount()
    {
        spawnObjects.RemoveAll(obj => obj == null);
        return spawnObjects.Count;
    }

    private void SpawnObject()
    {
        if (tilemap == null || validSpawnPositions.Count == 0) return;

        Vector3 spawnPos = validSpawnPositions[Random.Range(0, validSpawnPositions.Count)];
        if (PositionHasObject(spawnPos)) return;

        ObjectType type = RandomObjectType();

        if (objectPrefabs.Length > (int)type && objectPrefabs[(int)type] != null)
        {
            GameObject obj = Instantiate(
                objectPrefabs[(int)type],
                spawnPos + new Vector3(Random.Range(-randomOffsetX, randomOffsetX), 0, 0),
                Quaternion.identity
            );
            spawnObjects.Add(obj);

            if (type != ObjectType.Enemy)
                StartCoroutine(DestroyAfterTime(obj, gemLifeTime));
        }
    }
    private ObjectType RandomObjectType()
    {
        float total = enemyProbability + redGemProbability + greenGemProbability + blueGemProbability +
                      blackPearlProbability + goldenGemProbability;

        float rand = Random.value * total;
        float cumulative = 0;

        cumulative += enemyProbability;
        if (rand <= cumulative) return ObjectType.Enemy;

        cumulative += redGemProbability;
        if (rand <= cumulative) return ObjectType.RedGem;

        cumulative += greenGemProbability;
        if (rand <= cumulative) return ObjectType.GreenGem;

        cumulative += blueGemProbability;
        if (rand <= cumulative) return ObjectType.BlueGem;

        cumulative += blackPearlProbability;
        if (rand <= cumulative) return ObjectType.BlackPearl;

        return ObjectType.GoldenGem;
    }


    private bool PositionHasObject(Vector3 pos) =>
        spawnObjects.Any(obj => obj && Vector3.Distance(obj.transform.position, pos) < 0.5f);

    private IEnumerator DestroyAfterTime(GameObject obj, float t)
    {
        yield return new WaitForSeconds(t);
        if (obj != null)
        {
            spawnObjects.Remove(obj);
            Destroy(obj);
        }
    }

    public void DestroyAllSpawnedObjects()
    {
        foreach (var obj in spawnObjects)
            if (obj != null) Destroy(obj);
        spawnObjects.Clear();
    }

    public void GatherValidPositions()
    {
        validSpawnPositions.Clear();
        if (tilemap == null) return;

        BoundsInt bounds = tilemap.cellBounds;
        TileBase[] allTiles = tilemap.GetTilesBlock(bounds);

        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                TileBase tile = allTiles[x + y * bounds.size.x];
                if (tile != null)
                {
                    int aboveIndex = x + (y + 1) * bounds.size.x;
                    if (aboveIndex < allTiles.Length && allTiles[aboveIndex] == null)
                    {
                        Vector3Int tilePos = new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0);
                        Vector3 pos = tilemap.GetCellCenterWorld(tilePos);
                        pos.y += verticalOffset;
                        validSpawnPositions.Add(pos);
                    }
                }
            }
        }
    }
}

public enum ObjectType
{
    GreenGem = 0,
    RedGem = 1,
    BlueGem = 2,
    BlackPearl = 3,
    GoldenGem = 4,
    Enemy = 5
}
