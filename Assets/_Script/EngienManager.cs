using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EngienManager : MonoBehaviour
{
    [Header("Heat")]
    [SerializeField] private float maxHeat = 100f;
    [SerializeField] private float normalHeat = 0f;
    [SerializeField] private float heatIncreasePerSecond = 0.2f;
    [SerializeField] private float heatDecreasePerSecond = 0.05f;
    [SerializeField] private float fireStartHeat = 90f;

    [Header("Fire Spawning")]
    [SerializeField] private GameObject firePrefab;
    [Tooltip("Add your 10 empty fire-point transforms here.")]
    [SerializeField] private Transform[] fireSpawnPoints;
    [SerializeField] private int firesToSpawn = 5;

    [Header("Events")]
    [SerializeField] private UnityEvent onEngineFireStarted;
    [SerializeField] private UnityEvent onAllEngineFiresExtinguished;
    [Header("Heat Arrow")]
    [SerializeField] private Transform heatArrow;
    [SerializeField] private float arrowMinZ = 0f;
    [SerializeField] private float arrowMaxZ = 200f;
    public float CurrentHeat => currentHeat;
    public bool HasActiveFires => activeFires.Count > 0;

    private float currentHeat;
    private bool firesStarted;
    private readonly List<GameObject> activeFires = new List<GameObject>();

    private void Start()
    {
        currentHeat = normalHeat;
    }
    public void SetBoatMoving(bool isMoving)
    {
        if (HasActiveFires)
            return;

        if (isMoving)
        {
            currentHeat = Mathf.MoveTowards(
                currentHeat,
                maxHeat,
                heatIncreasePerSecond * Time.deltaTime
            );
        }
    }
    private void Update()
    {
        if (!HasActiveFires)
        {
            currentHeat = Mathf.MoveTowards(
                currentHeat,
                normalHeat,
                heatDecreasePerSecond * Time.deltaTime
            );
        }

        if (currentHeat >= fireStartHeat && !firesStarted)
        {
            StartEngineFire();
        }

        CheckFires();
        UpdateHeatArrow();
    }

    /// <summary>
    /// Call this from another script to make the engine hotter.
    /// Example: engineManager.IncreaseHeat(15f);
    /// </summary>
    public void IncreaseHeat(float amount)
    {
        if (HasActiveFires)
            return;

        currentHeat = Mathf.Clamp(currentHeat + amount, 0f, maxHeat);
    }

    public void SetHeat(float value)
    {
        if (HasActiveFires)
            return;

        currentHeat = Mathf.Clamp(value, 0f, maxHeat);
    }

    private void StartEngineFire()
    {
        if (firePrefab == null || fireSpawnPoints == null || fireSpawnPoints.Length == 0)
        {
            Debug.LogWarning("Engine Manager: Assign a Fire Prefab and Fire Spawn Points.");
            return;
        }

        firesStarted = true;
        activeFires.Clear();

        List<Transform> availablePoints = new List<Transform>();

        foreach (Transform point in fireSpawnPoints)
        {
            if (point != null)
                availablePoints.Add(point);
        }

        // Randomly shuffle the points, so no point is used twice.
        for (int i = 0; i < availablePoints.Count; i++)
        {
            int randomIndex = Random.Range(i, availablePoints.Count);

            Transform temp = availablePoints[i];
            availablePoints[i] = availablePoints[randomIndex];
            availablePoints[randomIndex] = temp;
        }

        int fireCount = Mathf.Min(firesToSpawn, availablePoints.Count);

        for (int i = 0; i < fireCount; i++)
        {
            Transform spawnPoint = availablePoints[i];

            GameObject fire = Instantiate(
                firePrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                spawnPoint
            );

            activeFires.Add(fire);
        }

        onEngineFireStarted?.Invoke();
    }

    private void CheckFires()
    {
        if (!firesStarted)
            return;

        activeFires.RemoveAll(fire => fire == null);

        if (activeFires.Count > 0)
            return;

        firesStarted = false;
        currentHeat = normalHeat;

        onAllEngineFiresExtinguished?.Invoke();
        Debug.Log("Fire end");
    }
    private void UpdateHeatArrow()
    {
        if (heatArrow == null)
            return;

        float heatRatio = Mathf.InverseLerp(0f, maxHeat, currentHeat);
        float zRotation = Mathf.Lerp(arrowMinZ, arrowMaxZ, heatRatio);

        Vector3 rotation = heatArrow.localEulerAngles;
        rotation.z = zRotation;
        heatArrow.localEulerAngles = rotation;
    }
    public void ForceStartEngineFire()
    {
        if (!firesStarted)
            StartEngineFire();
    }
}