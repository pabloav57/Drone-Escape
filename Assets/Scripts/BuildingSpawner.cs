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
    public float centralSafeWidth = 24f;
    public float safeLaneMargin = 8f;
    public float difficultyRampDuration = 120f;
    public int poolSize = 10;
    public int buildingsInRow = 20;
    public bool stylizeBuildings = true;
    public Color[] buildingPalette =
    {
        new Color(0.28f, 0.36f, 0.44f, 1f),
        new Color(0.38f, 0.43f, 0.48f, 1f),
        new Color(0.30f, 0.39f, 0.48f, 1f),
        new Color(0.44f, 0.40f, 0.36f, 1f)
    };
    public Color windowColor = new Color(0.82f, 0.92f, 1f, 1f);
    public Color warmWindowColor = new Color(1f, 0.78f, 0.45f, 1f);

    private float timeSinceLastSpawn;
    private float elapsedTime;
    private readonly Queue<GameObject> buildingPool = new Queue<GameObject>();
    private readonly List<GameObject> activeBuildings = new List<GameObject>();
    private float currentSpawnInterval;
    private float currentBuildingSpeed;
    private float difficultyMultiplier = 1f;
    private int selectedGameMode;
    private Material[] buildingMaterials;

    void Start()
    {
        selectedGameMode = PlayerPrefs.GetInt(GameModeKey, 0);
        SetDifficulty(PlayerPrefs.GetInt(DifficultyKey, 1));
        ApplyGameModeTuning();
        currentSpawnInterval = spawnInterval;
        currentBuildingSpeed = buildingSpeed;
        CreateBuildingMaterials();
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
        centralSafeWidth = Mathf.Max(24f, centralSafeWidth / difficultyMultiplier);
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
                centralSafeWidth = Mathf.Max(22f, centralSafeWidth * 0.9f);
                break;
            case 3: // Persecucion
                spawnInterval *= 1.1f;
                minSpawnInterval *= 1.05f;
                buildingSpeed *= 1.05f;
                maxBuildingSpeed *= 1.1f;
                centralSafeWidth = Mathf.Max(26f, centralSafeWidth * 1.15f);
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

            float width = Random.Range(minWidth, maxWidth);
            float xPosition = GetSpawnX(width);
            float zPosition = drone.position.z + spawnDistance + (i * maxWidth);
            SpawnSingleBuilding(building, xPosition, zPosition, width);
        }
    }

    void SpawnSingleBuilding(GameObject building, float xPosition, float zPosition, float width)
    {
        float height = Random.Range(minHeight, maxHeight);

        Vector3 spawnPosition = new Vector3(xPosition, height / 2f, zPosition);
        building.transform.position = spawnPosition;
        building.transform.localScale = new Vector3(width, height, width);
        ApplyBuildingStyle(building, height, width);

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

    float GetSpawnX(float buildingWidth)
    {
        float halfWidth = areaWidth * 0.5f;
        float halfSafeWidth = (centralSafeWidth * 0.5f) + (buildingWidth * 0.5f) + safeLaneMargin;
        halfSafeWidth = Mathf.Min(halfSafeWidth, halfWidth - 1f);

        if (Random.value > 0.5f)
        {
            return Random.Range(-halfWidth, -halfSafeWidth);
        }

        return Random.Range(halfSafeWidth, halfWidth);
    }

    private void CreateBuildingMaterials()
    {
        if (!stylizeBuildings || buildingPalette == null || buildingPalette.Length == 0)
        {
            return;
        }

        buildingMaterials = new Material[buildingPalette.Length];
        Shader shader = FindLitShader();

        for (int i = 0; i < buildingPalette.Length; i++)
        {
            Material material = new Material(shader);
            material.name = "Runtime_Building_Facade_" + i;
            material.color = buildingPalette[i];
            material.mainTexture = CreateFacadeTexture(buildingPalette[i], i);
            material.mainTextureScale = new Vector2(1f, 2f);
            buildingMaterials[i] = material;
        }
    }

    private void ApplyBuildingStyle(GameObject building, float height, float width)
    {
        if (!stylizeBuildings)
        {
            return;
        }

        Renderer renderer = building.GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            return;
        }

        if (buildingMaterials == null || buildingMaterials.Length == 0)
        {
            CreateBuildingMaterials();
        }

        if (buildingMaterials != null && buildingMaterials.Length > 0)
        {
            int materialIndex = Mathf.Abs(Mathf.RoundToInt((building.transform.position.x * 3f) + (building.transform.position.z * 0.1f))) % buildingMaterials.Length;
            renderer.material = buildingMaterials[materialIndex];
            renderer.material.mainTextureScale = new Vector2(Mathf.Max(1f, width * 0.12f), Mathf.Max(1f, height * 0.18f));
        }
    }

    private Texture2D CreateFacadeTexture(Color baseColor, int seed)
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Point;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool frame = x % 16 == 0 || y % 18 == 0;
                bool window = x % 16 > 4 && x % 16 < 12 && y % 18 > 5 && y % 18 < 13;
                float facadeNoise = Mathf.PerlinNoise((x + seed * 17) * 0.06f, (y + seed * 31) * 0.06f) * 0.12f;
                Color color = Color.Lerp(baseColor * 0.85f, baseColor * 1.15f, facadeNoise);

                if (frame)
                {
                    color = Color.Lerp(color, Color.black, 0.18f);
                }
                else if (window)
                {
                    bool warm = ((x / 16) + (y / 18) + seed) % 5 == 0;
                    bool lit = ((x / 16) * 7 + (y / 18) * 3 + seed) % 4 != 0;
                    color = lit ? Color.Lerp(color, warm ? warmWindowColor : windowColor, 0.72f) : Color.Lerp(color, Color.black, 0.25f);
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }

    private Shader FindLitShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader != null)
        {
            return shader;
        }

        shader = Shader.Find("Standard");
        return shader != null ? shader : Shader.Find("Sprites/Default");
    }
}
