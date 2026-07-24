using UnityEngine;

public class SharkSpawner : MonoBehaviour
{
    [SerializeField] private GameObject sharkPrefab;
    [SerializeField] private float respawnDelay = 10f;
    [SerializeField] private float spawnRadius = 60f;
    [SerializeField] private float spawnHeight = -3f;
    [SerializeField] private bool spawnOnStart = true;

    SharkEnemy activeShark;
    float respawnTimer;

    void Start()
    {
        if (spawnOnStart && sharkPrefab != null)
            SpawnShark();
    }

    void Update()
    {
        if (activeShark == null)
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f && sharkPrefab != null)
                SpawnShark();
        }
    }

    void SpawnShark()
    {
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        Vector3 spawnPos = transform.position + new Vector3(Mathf.Sin(angle) * spawnRadius, spawnHeight, Mathf.Cos(angle) * spawnRadius);

        GameObject obj = Instantiate(sharkPrefab, spawnPos, Quaternion.identity);
        activeShark = obj.GetComponent<SharkEnemy>();
        respawnTimer = respawnDelay;
    }
}
