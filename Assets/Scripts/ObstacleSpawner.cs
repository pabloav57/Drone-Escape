using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    private const string DifficultyKey = "SelectedDifficulty";
    private const string GameModeKey = "SelectedGameMode";

    public GameObject[] obstaclePrefabs;
    public GameObject erraticDronePrefab;
    public float baseSpawnInterval = 2f;
    public float obstacleDistance = 50f;
    public float minHeight = 1f;
    public float maxHeight = 10f;
    public Transform drone;
    public float baseObstacleSpeed = 5f;
    public float maxObstacleSpeed = 18f;
    public float minSpawnInterval = 0.8f;
    public float difficultyRampDuration = 90f;
    public int poolSize = 10;
    public float areaWidth = 50f;
    public int initialObstacleCount = 3;
    public int maxObstacleCount = 10;
    public int maxTotalDrones = 5;
    public float erraticDroneSpawnInterval = 8f;
    public float minErraticDroneSpawnInterval = 3.5f;

    private float timeSinceLastSpawn;
    private float timeSinceLastErraticDroneSpawn;
    private float currentSpawnInterval;
    private float currentObstacleSpeed;
    private float currentErraticDroneSpawnInterval;
    private float elapsedTime;
    private float difficultyMultiplier = 1f;
    private int selectedGameMode;

    private readonly Queue<GameObject> objectPool = new Queue<GameObject>();
    private readonly List<GameObject> activeErraticDrones = new List<GameObject>();

    void Start()
    {
        int savedDifficultyIndex = PlayerPrefs.GetInt(DifficultyKey, 1);
        selectedGameMode = PlayerPrefs.GetInt(GameModeKey, 0);
        SetDifficulty(savedDifficultyIndex);
        ApplyGameModeTuning();

        currentSpawnInterval = baseSpawnInterval;
        currentObstacleSpeed = baseObstacleSpeed;
        currentErraticDroneSpawnInterval = erraticDroneSpawnInterval;
        InitializeObjectPool();

        for (int i = 0; i < initialObstacleCount; i++)
        {
            SpawnObstacle();
        }
    }

    void Update()
    {
        if (drone == null)
        {
            return;
        }

        elapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsedTime / difficultyRampDuration);
        currentSpawnInterval = Mathf.Lerp(baseSpawnInterval, minSpawnInterval, progress);
        currentObstacleSpeed = Mathf.Lerp(baseObstacleSpeed, maxObstacleSpeed, progress);
        currentErraticDroneSpawnInterval = Mathf.Lerp(erraticDroneSpawnInterval, minErraticDroneSpawnInterval, progress);

        timeSinceLastSpawn += Time.deltaTime;
        if (timeSinceLastSpawn >= currentSpawnInterval && GetActiveObstacleCount() < maxObstacleCount)
        {
            SpawnObstacle();
            timeSinceLastSpawn = 0f;
        }

        timeSinceLastErraticDroneSpawn += Time.deltaTime;
        if (erraticDronePrefab != null && timeSinceLastErraticDroneSpawn >= currentErraticDroneSpawnInterval)
        {
            SpawnErraticDrone();
            timeSinceLastErraticDroneSpawn = 0f;
        }

        RemoveInactiveDrones();
    }

    void SetDifficulty(int difficultyIndex)
    {
        switch (difficultyIndex)
        {
            case 0:
                difficultyMultiplier = 0.8f;
                break;
            case 1:
                difficultyMultiplier = 1f;
                break;
            case 2:
                difficultyMultiplier = 1.2f;
                break;
            case 3:
                difficultyMultiplier = 1.4f;
                break;
        }

        baseSpawnInterval /= difficultyMultiplier;
        baseObstacleSpeed *= difficultyMultiplier;
        maxObstacleSpeed *= difficultyMultiplier;
        erraticDroneSpawnInterval /= difficultyMultiplier;
        minErraticDroneSpawnInterval = Mathf.Max(2f, minErraticDroneSpawnInterval / difficultyMultiplier);
        maxObstacleCount = Mathf.RoundToInt(maxObstacleCount * difficultyMultiplier);
    }

    void ApplyGameModeTuning()
    {
        switch (selectedGameMode)
        {
            case 1: // Zen
                baseSpawnInterval *= 1.6f;
                minSpawnInterval *= 1.5f;
                baseObstacleSpeed *= 0.75f;
                maxObstacleSpeed *= 0.8f;
                maxObstacleCount = Mathf.Max(4, Mathf.RoundToInt(maxObstacleCount * 0.6f));
                maxTotalDrones = 0;
                break;
            case 2: // Rush
                baseSpawnInterval *= 0.75f;
                minSpawnInterval *= 0.7f;
                baseObstacleSpeed *= 1.2f;
                maxObstacleSpeed *= 1.25f;
                maxObstacleCount = Mathf.RoundToInt(maxObstacleCount * 1.25f);
                erraticDroneSpawnInterval *= 0.8f;
                minErraticDroneSpawnInterval *= 0.8f;
                break;
        }
    }

    void InitializeObjectPool()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
        {
            Debug.LogError("No hay prefabs de obstaculos asignados.");
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)]);
            obj.SetActive(false);
            EnsureObstacleSetup(obj);
            objectPool.Enqueue(obj);
        }
    }

    void RemoveInactiveDrones()
    {
        for (int i = activeErraticDrones.Count - 1; i >= 0; i--)
        {
            GameObject activeDrone = activeErraticDrones[i];

            if (activeDrone == null)
            {
                activeErraticDrones.RemoveAt(i);
                continue;
            }

            if (activeDrone.transform.position.z < drone.position.z - 25f)
            {
                activeErraticDrones.RemoveAt(i);
                Destroy(activeDrone);
            }
        }
    }

    void SpawnObstacle()
    {
        if (drone == null || obstaclePrefabs == null || obstaclePrefabs.Length == 0)
        {
            return;
        }

        GameObject newObstacle = objectPool.Count > 0
            ? objectPool.Dequeue()
            : Instantiate(obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)]);

        newObstacle.SetActive(true);

        float obstacleZPosition = drone.position.z + obstacleDistance + Random.Range(10f, 30f);
        float obstacleYPosition = Random.Range(minHeight, maxHeight);
        float obstacleXPosition = drone.position.x + Random.Range(-areaWidth / 2f, areaWidth / 2f);

        newObstacle.transform.position = new Vector3(obstacleXPosition, obstacleYPosition, obstacleZPosition);
        EnsureObstacleSetup(newObstacle);
    }

    void SpawnErraticDrone()
    {
        if (activeErraticDrones.Count >= maxTotalDrones || drone == null)
        {
            return;
        }

        GameObject erraticDrone = Instantiate(erraticDronePrefab);
        float spawnZ = drone.position.z + obstacleDistance + Random.Range(10f, 20f);
        float spawnX = drone.position.x + Random.Range(-3f, 3f);
        float spawnY = Random.Range(minHeight, maxHeight);

        erraticDrone.transform.position = new Vector3(spawnX, spawnY, spawnZ);

        ErraticDroneMovement erraticMovement = erraticDrone.GetComponent<ErraticDroneMovement>();
        if (erraticMovement == null)
        {
            erraticMovement = erraticDrone.AddComponent<ErraticDroneMovement>();
        }

        erraticMovement.targetDrone = drone;
        erraticMovement.speed = currentObstacleSpeed * 0.6f;
        erraticMovement.verticalAmplitude = 3f;
        erraticMovement.verticalSpeed = 1.5f;
        erraticMovement.SetUpMovement();

        activeErraticDrones.Add(erraticDrone);
    }

    private void EnsureObstacleSetup(GameObject obstacle)
    {
        ObstacleMovement obstacleMovement = obstacle.GetComponent<ObstacleMovement>();
        if (obstacleMovement == null)
        {
            obstacleMovement = obstacle.AddComponent<ObstacleMovement>();
        }

        obstacleMovement.speed = currentObstacleSpeed;
        obstacleMovement.drone = drone;

        if (obstacle.GetComponent<ObstacleMarker>() == null)
        {
            obstacle.AddComponent<ObstacleMarker>();
        }
    }

    private int GetActiveObstacleCount()
    {
        return FindObjectsByType<ObstacleMarker>().Length;
    }
}

public class ObstacleMarker : MonoBehaviour
{
}
