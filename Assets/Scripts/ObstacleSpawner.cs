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
    public float verticalSpawnRange = 6f;
    public float spawnAccelerationInterval = 15f;
    public float spawnAccelerationPerStep = 0.08f;
    public float speedAccelerationPerStep = 0.07f;
    public Transform drone;
    public float baseObstacleSpeed = 5f;
    public float maxObstacleSpeed = 18f;
    public float minSpawnInterval = 0.8f;
    public float difficultyRampDuration = 90f;
    public int poolSize = 10;
    public float areaWidth = 50f;
    public float obstacleLaneHalfWidth = 12f;
    public int initialObstacleCount = 3;
    public int maxObstacleCount = 10;
    public int maxTotalDrones = 5;
    public float erraticDroneSpawnInterval = 8f;
    public float minErraticDroneSpawnInterval = 3.5f;
    public GameController gameController;

    private float timeSinceLastSpawn;
    private float timeSinceLastErraticDroneSpawn;
    private float currentSpawnInterval;
    private float currentObstacleSpeed;
    private float currentErraticDroneSpawnInterval;
    private float elapsedTime;
    private float difficultyMultiplier = 1f;
    private int selectedGameMode;
    private int savedDifficultyIndex = 1;
    private float effectiveMinHeight;
    private float effectiveMaxHeight;

    private readonly Queue<GameObject> objectPool = new Queue<GameObject>();
    private readonly List<GameObject> activeErraticDrones = new List<GameObject>();

    public float CurrentObstacleSpeed => currentObstacleSpeed;
    public float DifficultyProgress => Mathf.Clamp01(elapsedTime / difficultyRampDuration);

    void Start()
    {
        savedDifficultyIndex = PlayerPrefs.GetInt(DifficultyKey, 1);
        selectedGameMode = PlayerPrefs.GetInt(GameModeKey, 0);
        if (gameController == null)
        {
            gameController = FindAnyObjectByType<GameController>();
        }

        ClearSceneErraticDrones();
        ResolvePlayableHeightRange();
        SetDifficulty(savedDifficultyIndex);
        ApplyGameModeTuning();

        currentSpawnInterval = baseSpawnInterval;
        currentObstacleSpeed = baseObstacleSpeed;
        currentErraticDroneSpawnInterval = selectedGameMode == 3 ? erraticDroneSpawnInterval : float.PositiveInfinity;
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
        float timedDifficulty = GetTimedDifficultyMultiplier();
        currentSpawnInterval = Mathf.Max(minSpawnInterval, Mathf.Lerp(baseSpawnInterval, minSpawnInterval, progress) / timedDifficulty);
        currentObstacleSpeed = Mathf.Min(maxObstacleSpeed * 1.35f, Mathf.Lerp(baseObstacleSpeed, maxObstacleSpeed, progress) * timedDifficulty);
        currentErraticDroneSpawnInterval = selectedGameMode == 3
            ? Mathf.Lerp(erraticDroneSpawnInterval, minErraticDroneSpawnInterval, progress)
            : float.PositiveInfinity;

        timeSinceLastSpawn += Time.deltaTime;
        if (timeSinceLastSpawn >= currentSpawnInterval && GetActiveObstacleCount() < maxObstacleCount)
        {
            SpawnObstacle();
            timeSinceLastSpawn = 0f;
        }

        timeSinceLastErraticDroneSpawn += Time.deltaTime;
        if (selectedGameMode == 3 && erraticDronePrefab != null && timeSinceLastErraticDroneSpawn >= currentErraticDroneSpawnInterval)
        {
            SpawnErraticDrone();
            timeSinceLastErraticDroneSpawn = 0f;
        }

        RemoveInactiveDrones();
        UpdatePursuitHud();
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
        maxObstacleCount = Mathf.Clamp(Mathf.RoundToInt(maxObstacleCount * difficultyMultiplier), 4, 8);
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
                maxObstacleCount = Mathf.Clamp(Mathf.RoundToInt(maxObstacleCount * 0.6f), 3, 5);
                maxTotalDrones = 0;
                break;
            case 2: // Rush
                baseSpawnInterval *= 0.75f;
                minSpawnInterval *= 0.7f;
                baseObstacleSpeed *= 1.2f;
                maxObstacleSpeed *= 1.25f;
                maxObstacleCount = Mathf.Clamp(Mathf.RoundToInt(maxObstacleCount * 1.15f), 5, 8);
                maxTotalDrones = 0;
                break;
            case 3: // Persecucion
                baseSpawnInterval *= 1.15f;
                minSpawnInterval *= 1.1f;
                baseObstacleSpeed *= 1.05f;
                maxObstacleSpeed *= 1.12f;
                maxObstacleCount = Mathf.Clamp(Mathf.RoundToInt(maxObstacleCount * 0.75f), 4, 6);
                maxTotalDrones = 1;
                erraticDroneSpawnInterval = 16f;
                minErraticDroneSpawnInterval = 13f;
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

    void ClearSceneErraticDrones()
    {
        ErraticDroneMovement[] sceneDrones = FindObjectsByType<ErraticDroneMovement>(FindObjectsInactive.Include);
        for (int i = 0; i < sceneDrones.Length; i++)
        {
            if (sceneDrones[i] != null)
            {
                Destroy(sceneDrones[i].gameObject);
            }
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

        newObstacle.transform.position = GetObstacleSpawnPosition(obstacleDistance + 10f, obstacleDistance + 30f);
        EnsureObstacleSetup(newObstacle);
    }

    public Vector3 GetObstacleSpawnPosition(float minDistanceAhead = 80f, float maxDistanceAhead = 120f)
    {
        if (drone == null)
        {
            return Vector3.zero;
        }

        float obstacleZPosition = drone.position.z + Random.Range(minDistanceAhead, maxDistanceAhead);
        float obstacleYPosition = GetSpawnYNearDrone();
        float obstacleXPosition = GetSpawnXNearDrone();

        return new Vector3(obstacleXPosition, obstacleYPosition, obstacleZPosition);
    }

    public float ClampObstacleX(float xPosition)
    {
        if (drone == null)
        {
            return xPosition;
        }

        return Mathf.Clamp(xPosition, drone.position.x - obstacleLaneHalfWidth, drone.position.x + obstacleLaneHalfWidth);
    }

    private float GetSpawnXNearDrone()
    {
        if (drone == null)
        {
            return 0f;
        }

        return drone.position.x + Random.Range(-obstacleLaneHalfWidth, obstacleLaneHalfWidth);
    }

    public float GetSpawnYNearDrone()
    {
        ResolvePlayableHeightRange();

        float halfRange = verticalSpawnRange * 0.5f;
        float centerY = drone != null ? drone.position.y : Mathf.Lerp(effectiveMinHeight, effectiveMaxHeight, 0.5f);
        float minSpawnY = Mathf.Max(effectiveMinHeight, centerY - halfRange);
        float maxSpawnY = Mathf.Min(effectiveMaxHeight, centerY + halfRange);

        if (maxSpawnY - minSpawnY < 1.5f)
        {
            if (centerY >= (effectiveMinHeight + effectiveMaxHeight) * 0.5f)
            {
                minSpawnY = Mathf.Max(effectiveMinHeight, effectiveMaxHeight - verticalSpawnRange);
                maxSpawnY = effectiveMaxHeight;
            }
            else
            {
                minSpawnY = effectiveMinHeight;
                maxSpawnY = Mathf.Min(effectiveMaxHeight, effectiveMinHeight + verticalSpawnRange);
            }
        }

        return Random.Range(minSpawnY, maxSpawnY);
    }

    private void ResolvePlayableHeightRange()
    {
        effectiveMinHeight = minHeight;
        effectiveMaxHeight = maxHeight;

        if (drone == null)
        {
            return;
        }

        DroneController droneController = drone.GetComponent<DroneController>();
        if (droneController == null)
        {
            return;
        }

        effectiveMinHeight = Mathf.Min(minHeight, droneController.minHeight);
        effectiveMaxHeight = Mathf.Max(maxHeight, droneController.maxHeight);
    }

    private float GetTimedDifficultyMultiplier()
    {
        int accelerationSteps = Mathf.FloorToInt(elapsedTime / Mathf.Max(1f, spawnAccelerationInterval));
        float difficultyWeight = 1f + (savedDifficultyIndex * 0.35f);
        float spawnBoost = 1f + (accelerationSteps * spawnAccelerationPerStep * difficultyWeight);
        float speedBoost = 1f + (accelerationSteps * speedAccelerationPerStep * difficultyWeight);
        return Mathf.Min(spawnBoost * speedBoost, 2.4f);
    }

    void SpawnErraticDrone()
    {
        if (activeErraticDrones.Count >= maxTotalDrones || drone == null)
        {
            return;
        }

        GameObject erraticDrone = Instantiate(erraticDronePrefab);
        float spawnZ = selectedGameMode == 3
            ? drone.position.z + Random.Range(18f, 28f)
            : drone.position.z + obstacleDistance + Random.Range(10f, 20f);
        float spawnX = selectedGameMode == 3
            ? drone.position.x + Random.Range(-10f, 10f)
            : drone.position.x + Random.Range(-3f, 3f);
        float spawnY = selectedGameMode == 3
            ? Mathf.Clamp(drone.position.y + Random.Range(-2f, 3f), effectiveMinHeight, effectiveMaxHeight)
            : Random.Range(effectiveMinHeight, effectiveMaxHeight);

        erraticDrone.transform.position = new Vector3(spawnX, spawnY, spawnZ);
        ConfigureErraticDroneHazard(erraticDrone);

        ErraticDroneMovement erraticMovement = erraticDrone.GetComponent<ErraticDroneMovement>();
        if (erraticMovement == null)
        {
            erraticMovement = erraticDrone.AddComponent<ErraticDroneMovement>();
        }

        erraticMovement.targetDrone = drone;
        erraticMovement.speed = currentObstacleSpeed * (selectedGameMode == 3 ? 0.95f : 0.6f);
        erraticMovement.verticalAmplitude = selectedGameMode == 3 ? 2f : 3f;
        erraticMovement.verticalSpeed = selectedGameMode == 3 ? 2.2f : 1.5f;
        erraticMovement.destabilizesTarget = selectedGameMode == 3;
        erraticMovement.destabilizeRadius = selectedGameMode == 3 ? 9f : 5f;
        erraticMovement.destabilizeStrength = selectedGameMode == 3 ? 0.42f : 0.18f;
        erraticMovement.followDistance = selectedGameMode == 3 ? 4f : 0f;
        erraticMovement.sideOffset = selectedGameMode == 3 ? 4.5f : 1.5f;
        erraticMovement.pursuitDuration = selectedGameMode == 3 ? 10f : 999f;
        erraticMovement.SetUpMovement();

        activeErraticDrones.Add(erraticDrone);
    }

    private void UpdatePursuitHud()
    {
        if (gameController == null || selectedGameMode != 3)
        {
            return;
        }

        float remainingTime = 0f;
        for (int i = 0; i < activeErraticDrones.Count; i++)
        {
            if (activeErraticDrones[i] == null)
            {
                continue;
            }

            ErraticDroneMovement movement = activeErraticDrones[i].GetComponent<ErraticDroneMovement>();
            if (movement != null && movement.ShouldShowAlert)
            {
                remainingTime = Mathf.Max(remainingTime, movement.ThreatRemainingTime);
            }
        }

        gameController.SetPursuitStatus(remainingTime > 0f, remainingTime);
    }

    private void ConfigureErraticDroneHazard(GameObject erraticDrone)
    {
        if (erraticDrone == null)
        {
            return;
        }

        erraticDrone.tag = "Obstacle";

        Rigidbody body = erraticDrone.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = erraticDrone.AddComponent<Rigidbody>();
        }

        body.isKinematic = true;
        body.useGravity = false;

        Collider[] colliders = erraticDrone.GetComponentsInChildren<Collider>(true);
        if (colliders.Length == 0)
        {
            SphereCollider sphereCollider = erraticDrone.AddComponent<SphereCollider>();
            sphereCollider.radius = 1.6f;
            colliders = new Collider[] { sphereCollider };
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].isTrigger = true;
            }
        }
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
        obstacleMovement.gameController = gameController;
        obstacleMovement.obstacleSpawner = this;
        obstacleMovement.ConfigureMovementPattern(DifficultyProgress);

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
