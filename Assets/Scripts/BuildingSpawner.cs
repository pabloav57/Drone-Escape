using System.Collections.Generic;
using UnityEngine;

public class BuildingSpawner : MonoBehaviour
{
    private const string DifficultyKey = "SelectedDifficulty";
    private const string GameModeKey = "SelectedGameMode";

    public GameObject buildingPrefab;
    public Transform drone;
    public float spawnInterval = 3f;
    public float minSpawnInterval = 1f;
    public float spawnDistance = 100f;
    public float buildingSpeed = 5f;
    public float maxBuildingSpeed = 12f;
    public float minHeight = 10f;
    public float maxHeight = 30f;
    public float minWidth = 5f;
    public float maxWidth = 15f;
    public float areaWidth = 50f;
    public float centralSafeWidth = 8f;
    public float difficultyRampDuration = 120f;
    public int poolSize = 10;
    public int buildingsInRow = 20;

    private float timeSinceLastSpawn;
    private float elapsedTime;
    private readonly Queue<GameObject> buildingPool = new Queue<GameObject>();
    private readonly List<GameObject> activeBuildings = new List<GameObject>();
    private float currentSpawnInterval;
    private float currentBuildingSpeed;
    private float difficultyMultiplier = 1f;
    private int selectedGameMode;

    void Start()
    {
        selectedGameMode = PlayerPrefs.GetInt(GameModeKey, 0);
        SetDifficulty(PlayerPrefs.GetInt(DifficultyKey, 1));
        ApplyGameModeTuning();
        currentSpawnInterval = spawnInterval;
        currentBuildingSpeed = buildingSpeed;
        InitializeBuildingPool();
    }

    void Update()
    {
        if (drone == null || buildingPrefab == null)
        {
            return;
        }

        elapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsedTime / difficultyRampDuration);
        currentSpawnInterval = Mathf.Lerp(spawnInterval, minSpawnInterval, progress);
        currentBuildingSpeed = Mathf.Lerp(buildingSpeed, maxBuildingSpeed, progress);

        timeSinceLastSpawn += Time.deltaTime;
        if (timeSinceLastSpawn >= currentSpawnInterval)
        {
            SpawnRowOfBuildings();
            timeSinceLastSpawn = 0f;
        }

        MoveBuildings();
    }

    void SetDifficulty(int difficultyIndex)
    {
        switch (difficultyIndex)
        {
            case 0:
                difficultyMultiplier = 0.85f;
                break;
            case 1:
                difficultyMultiplier = 1f;
                break;
            case 2:
                difficultyMultiplier = 1.15f;
                break;
            case 3:
                difficultyMultiplier = 1.3f;
                break;
        }

        spawnInterval /= difficultyMultiplier;
        minSpawnInterval = Mathf.Max(0.8f, minSpawnInterval / difficultyMultiplier);
        buildingSpeed *= difficultyMultiplier;
        maxBuildingSpeed *= difficultyMultiplier;
        centralSafeWidth = Mathf.Max(5f, centralSafeWidth / difficultyMultiplier);
    }

    void ApplyGameModeTuning()
    {
        switch (selectedGameMode)
        {
            case 1: // Zen
                spawnInterval *= 1.35f;
                minSpawnInterval *= 1.4f;
                buildingSpeed *= 0.8f;
                maxBuildingSpeed *= 0.85f;
                centralSafeWidth *= 1.35f;
                break;
            case 2: // Rush
                spawnInterval *= 0.85f;
                minSpawnInterval *= 0.85f;
                buildingSpeed *= 1.15f;
                maxBuildingSpeed *= 1.2f;
                centralSafeWidth = Mathf.Max(4f, centralSafeWidth * 0.8f);
                break;
        }
    }

    void InitializeBuildingPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject building = Instantiate(buildingPrefab);
            building.SetActive(false);
            buildingPool.Enqueue(building);
        }
    }

    void SpawnRowOfBuildings()
    {
        for (int i = 0; i < buildingsInRow; i++)
        {
            GameObject building = buildingPool.Count > 0 ? buildingPool.Dequeue() : Instantiate(buildingPrefab);
            building.SetActive(true);

            float xPosition = GetSpawnX();
            float zPosition = drone.position.z + spawnDistance + (i * maxWidth);
            SpawnSingleBuilding(building, xPosition, zPosition);
        }
    }

    void SpawnSingleBuilding(GameObject building, float xPosition, float zPosition)
    {
        float height = Random.Range(minHeight, maxHeight);
        float width = Random.Range(minWidth, maxWidth);

        Vector3 spawnPosition = new Vector3(xPosition, height / 2f, zPosition);
        building.transform.position = spawnPosition;
        building.transform.localScale = new Vector3(width, height, width);

        BoxCollider collider = building.GetComponent<BoxCollider>() ?? building.AddComponent<BoxCollider>();
        collider.size = Vector3.one;
        collider.center = Vector3.up * 0.5f;
        collider.isTrigger = false;

        if (!activeBuildings.Contains(building))
        {
            activeBuildings.Add(building);
        }
    }

    void MoveBuildings()
    {
        for (int i = activeBuildings.Count - 1; i >= 0; i--)
        {
            GameObject building = activeBuildings[i];
            building.transform.Translate(Vector3.back * currentBuildingSpeed * Time.deltaTime, Space.World);

            if (building.transform.position.z < drone.position.z - 20f)
            {
                activeBuildings.RemoveAt(i);
                ReturnBuildingToPool(building);
            }
        }
    }

    void ReturnBuildingToPool(GameObject building)
    {
        building.SetActive(false);
        buildingPool.Enqueue(building);
    }

    float GetSpawnX()
    {
        float halfWidth = areaWidth * 0.5f;
        float halfSafeWidth = centralSafeWidth * 0.5f;

        if (Random.value > 0.5f)
        {
            return Random.Range(-halfWidth, -halfSafeWidth);
        }

        return Random.Range(halfSafeWidth, halfWidth);
    }
}
