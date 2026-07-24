using System.Collections.Generic;
using UnityEngine;

public class OceanGenerator : MonoBehaviour
{
    [Header("Chunk Settings")]
    [SerializeField] private float chunkLength = 33f;
    [SerializeField] private float chunkWidth = 33f;
    [SerializeField] private int viewDistance = 3;

    [Header("Water")]
    [SerializeField] private GameObject waterPrefab;

    [Header("Obstacles")]
    [SerializeField] private GameObject[] rockPrefabs;
    [SerializeField] private int minRocksPerChunk = 3;
    [SerializeField] private int maxRocksPerChunk = 7;
    [SerializeField] private float rowSpacing = 18f;
    [SerializeField] private int lanes = 5;
    [SerializeField] private float minRockScale = 0.8f;
    [SerializeField] private float maxRockScale = 1.5f;

    private Transform player;
    private int lastChunkIndex;
    private int startChunkIndex;
    private Dictionary<int, GameObject> spawnedChunks = new Dictionary<int, GameObject>();

    public bool showGizmos = false;

    void Start()
    {
        var playerCore = FindFirstObjectByType<PlayerCore>();
        if (playerCore != null) player = playerCore.transform;

        if (player != null)
        {
            lastChunkIndex = GetChunkIndex(player.position.z);
            startChunkIndex = lastChunkIndex;
            GenerateInitialChunks();
        }
    }

    void Update()
    {
        if (player == null) return;

        int currentChunkIndex = GetChunkIndex(player.position.z);

        if (currentChunkIndex != lastChunkIndex)
        {
            lastChunkIndex = currentChunkIndex;
            SpawnAhead();
            DespawnBehind();
        }
    }

    int GetChunkIndex(float z)
    {
        return Mathf.FloorToInt(z / chunkLength);
    }

    void GenerateInitialChunks()
    {
        for (int i = lastChunkIndex - 1; i <= lastChunkIndex + viewDistance; i++)
        {
            SpawnChunk(i);
        }
    }

    void SpawnAhead()
    {
        for (int i = lastChunkIndex; i <= lastChunkIndex + viewDistance; i++)
        {
            if (!spawnedChunks.ContainsKey(i))
            {
                SpawnChunk(i);
            }
        }
    }

    void DespawnBehind()
    {
        int behindIndex = lastChunkIndex - 2;
        List<int> toRemove = new List<int>();

        foreach (var kvp in spawnedChunks)
        {
            if (kvp.Key < behindIndex)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (int index in toRemove)
        {
            Destroy(spawnedChunks[index]);
            spawnedChunks.Remove(index);
        }
    }

    void SpawnChunk(int index)
    {
        if (spawnedChunks.ContainsKey(index)) return;

        float chunkStartZ = index * chunkLength;
        Vector3 chunkCenter = new Vector3(0f, 0f, chunkStartZ + chunkLength * 0.5f);

        GameObject chunkObj = new GameObject($"Chunk_{index}");
        chunkObj.transform.position = chunkCenter;

        if (waterPrefab != null)
        {
            GameObject water = Instantiate(waterPrefab, chunkObj.transform);
            water.transform.localPosition = Vector3.zero;
        }

        PlaceRocks(chunkObj.transform, index);

        spawnedChunks[index] = chunkObj;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        foreach (var kvp in spawnedChunks)
        {
            float startZ = kvp.Key * chunkLength;
            Vector3 center = new Vector3(0f, 0f, startZ + chunkLength * 0.5f);
            Gizmos.DrawCube(center, new Vector3(chunkWidth, 2f, chunkLength));
        }
    }

    void PlaceRocks(Transform chunkParent, int chunkIndex)
    {
        if (chunkIndex <= startChunkIndex + viewDistance) return;

        float chunkStartZ = chunkIndex * chunkLength;
        float rowCount = Mathf.Floor(chunkLength / rowSpacing);
        int rockCount = Random.Range(minRocksPerChunk, maxRocksPerChunk + 1);
        int placed = 0;

        for (int row = 0; row < (int)rowCount && placed < rockCount; row++)
        {
            float rowZ = chunkStartZ + (row + 0.5f) * rowSpacing;

            List<int> availableLanes = new List<int>();
            for (int i = 0; i < lanes; i++) availableLanes.Add(i);

            int rocksInRow = Random.Range(1, Mathf.Min(3, lanes));
            rocksInRow = Mathf.Min(rocksInRow, availableLanes.Count - 1);

            for (int r = 0; r < rocksInRow && placed < rockCount; r++)
            {
                int laneIndex = Random.Range(0, availableLanes.Count);
                int lane = availableLanes[laneIndex];
                availableLanes.RemoveAt(laneIndex);

                float laneWidth = chunkWidth / lanes;
                float x = (lane - (lanes - 1) * 0.5f) * laneWidth;
                x += Random.Range(-laneWidth * 0.2f, laneWidth * 0.2f);

                float scale = Random.Range(minRockScale, maxRockScale);

                Vector3 localPos = new Vector3(x, 0f, rowZ - chunkParent.position.z);
                GameObject prefab = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
                GameObject rock = Instantiate(prefab, chunkParent);
                rock.transform.localPosition = localPos;
                rock.transform.localScale = Vector3.one * scale;
                rock.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                placed++;
            }
        }
    }
}
