using UnityEngine;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject[] obstaclePrefabs; // Prefabs de obstáculos
    public float baseSpawnInterval = 2f; // Intervalo base entre spawns
    public float obstacleDistance = 50f; // Distancia inicial para generar obstáculos
    public float minHeight = 1f; // Altura mínima de obstáculos
    public float maxHeight = 10f; // Altura máxima de obstáculos
    public Transform drone; // Referencia al dron
    public float baseObstacleSpeed = 5f; // Velocidad base de los obstáculos
    public float difficultyIncreaseRate = 0.05f; // Incremento por segundo en dificultad
    public int poolSize = 10; // Tamaño del pool de objetos
    public int initialObstacleCount = 3; // Número inicial de obstáculos
    public int maxObstacleCount = 10; // Número máximo de obstáculos

    private float timeSinceLastSpawn = 0f;
    private float currentSpawnInterval;
    private float currentObstacleSpeed;
    private float elapsedTime = 0f;
    private int currentObstacleCount = 0; // Contador de obstáculos generados
    private Queue<GameObject> objectPool = new Queue<GameObject>();

    public GameObject erraticDronePrefab;  // Prefab del drone errático
    public float erraticDroneSpawnRate = 10f;  // Probabilidad de que aparezca un drone errático

    public int maxTotalDrones = 10; // Límite total de drones en pantalla (erráticos + no erráticos)
    private int activeDrones = 0;  // Contador total de drones activos en pantalla


    void Start()
    {
        int difficultyIndex = PlayerPrefs.GetInt("SelectedDifficulty", 1); // 1 es el valor predeterminado (Normal)
        SetDifficulty(difficultyIndex);

        currentSpawnInterval = baseSpawnInterval;
        currentObstacleSpeed = baseObstacleSpeed;
        InitializeObjectPool(); // Inicializar el pool de objetos

        // Generar algunos obstáculos al principio
        for (int i = 0; i < initialObstacleCount; i++)
        {
            SpawnObstacle();
        }
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;

        // Incrementar dificultad con el tiempo
        currentSpawnInterval = Mathf.Max(1f, baseSpawnInterval - (elapsedTime * difficultyIncreaseRate));
        currentObstacleSpeed = Mathf.Min(10f, baseObstacleSpeed + (elapsedTime * difficultyIncreaseRate));

        // Controlar el tiempo entre spawns
        timeSinceLastSpawn += Time.deltaTime;
        if (timeSinceLastSpawn >= currentSpawnInterval && currentObstacleCount < maxObstacleCount)
        {
            SpawnObstacle();
            timeSinceLastSpawn = 0f;
        }
    }

    void SetDifficulty(int difficultyIndex)
    {
        switch (difficultyIndex)
        {
            case 0: // Fácil
                baseSpawnInterval = 3f; // Intervalo más largo
                baseObstacleSpeed = 10f; // Velocidad más baja
                break;
            case 1: // Normal
                baseSpawnInterval = 2f; // Intervalo medio
                baseObstacleSpeed = 20f; // Velocidad media
                break;
            case 2: // Difícil
                baseSpawnInterval = 1.5f; // Intervalo más corto
                baseObstacleSpeed = 50f; // Velocidad más alta
                break;
            case 3: // Imposible
                baseSpawnInterval = 1f; // Intervalo muy corto
                baseObstacleSpeed = 100f; // Velocidad máxima
                break;
        }
    }

    void InitializeObjectPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)]);
            obj.SetActive(false); 
            objectPool.Enqueue(obj);
        }
    }

    void SpawnObstacle()
    {
        GameObject newObstacle;

        // Usar un objeto del pool o crear uno nuevo
        if (objectPool.Count > 0)
        {
            newObstacle = objectPool.Dequeue();
            newObstacle.SetActive(true);
        }
        else
        {
            newObstacle = Instantiate(obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)]);
        }

        // **Probabilidad de aparición del drone errático**:
        if (Random.Range(0f, 100f) <= erraticDroneSpawnRate)
        {
            // Instanciar el drone errático
            SpawnErraticDrone();
        }

        // Ajustamos la posición del obstáculo en función de la posición del drone y su trayectoria
        float obstacleZPosition = drone.position.z + obstacleDistance;
        float obstacleYPosition = Random.Range(minHeight, maxHeight);
        float obstacleXPosition = drone.position.x + Random.Range(-5f, 5f); // Variación lateral

        newObstacle.transform.position = new Vector3(obstacleXPosition, obstacleYPosition, obstacleZPosition);

        // Asignamos el movimiento
        ObstacleMovement obstacleMovement = newObstacle.GetComponent<ObstacleMovement>();
        if (obstacleMovement == null)
        {
            obstacleMovement = newObstacle.AddComponent<ObstacleMovement>();
        }
        obstacleMovement.speed = currentObstacleSpeed;

        // Asignamos la referencia al drone
        obstacleMovement.drone = drone;

        // Incrementar el contador de obstáculos
        currentObstacleCount++;
    }

    void SpawnErraticDrone()
    {
        // Generación dinámica del drone errático
        GameObject erraticDrone = Instantiate(erraticDronePrefab);
        
        float erraticDroneZPosition = drone.position.z + obstacleDistance;
        float erraticDroneXPosition = drone.position.x + Random.Range(-5f, 5f); // Variación lateral
        float erraticDroneYPosition = Random.Range(minHeight, maxHeight);

        erraticDrone.transform.position = new Vector3(erraticDroneXPosition, erraticDroneYPosition, erraticDroneZPosition);

        ErraticDroneMovement erraticMovement = erraticDrone.GetComponent<ErraticDroneMovement>();
        if (erraticMovement != null)
        {
            erraticMovement.targetDrone = drone;  // Asignamos el dron principal
            erraticMovement.SetUpMovement(); // Inicializa el movimiento
        }
    }
}
