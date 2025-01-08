using UnityEngine;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject[] obstaclePrefabs; // Prefabs de obstáculos
    public GameObject erraticDronePrefab; // Prefab del dron errático
    public float baseSpawnInterval = 2f; // Intervalo base entre spawns
    public float obstacleDistance = 50f; // Distancia inicial para generar obstáculos
    public float minHeight = 1f; // Altura mínima de obstáculos
    public float maxHeight = 10f; // Altura máxima de obstáculos
    public Transform drone; // Referencia al dron
    public float baseObstacleSpeed = 5f; // Velocidad base de los obstáculos
    public float difficultyIncreaseRate = 0.05f; // Incremento por segundo en dificultad
    public int poolSize = 10; // Tamaño del pool de objetos
    public float areaWidth = 50f; // Ancho del área de generación
    public int initialObstacleCount = 3; // Número inicial de obstáculos
    public int maxObstacleCount = 10; // Número máximo de obstáculos
    public int maxTotalDrones = 5; // Número máximo de drones activos

    private float timeSinceLastSpawn = 0f;
    private float currentSpawnInterval;
    private float currentObstacleSpeed;
    private float elapsedTime = 0f;
    private int currentObstacleCount = 0; // Contador de obstáculos generados
    private int activeDrones = 0; // Contador de drones activos

    private Queue<GameObject> objectPool = new Queue<GameObject>();

    void Start()
    {
        int savedDifficultyIndex = PlayerPrefs.GetInt("SelectedDifficulty", 1); // 1 como valor por defecto, si no hay valor guardado
        SetDifficulty(savedDifficultyIndex); // Aplicar dificultad guardada

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

        // Eliminar drones inactivos
        RemoveInactiveDrones();
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

    void RemoveInactiveDrones()
    {
        // Recorremos todos los drones activos y los eliminamos si están fuera de la pantalla
        GameObject[] drones = GameObject.FindGameObjectsWithTag("Drone"); // Opción: asumir que todos los drones tienen la etiqueta "Drone"
        foreach (GameObject drone in drones)
        {
            if (drone.transform.position.z < drone.transform.position.z - 10f) // Ajustar el valor si es necesario
            {
                Destroy(drone); // Destruir el dron o regresarlo al pool si lo prefieres
                activeDrones--; // Disminuir el contador de drones activos
            }
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

        // Ajustamos la posición del obstáculo en función de la orientación del dron
        Vector3 forwardDirection = drone.forward; // Dirección hacia donde está mirando el dron
        Vector3 rightDirection = drone.right; // Dirección lateral derecha
        float obstacleZPosition = drone.position.z + obstacleDistance; // Posición Z siempre frente al dron
        float obstacleYPosition = Random.Range(minHeight, maxHeight);

        // Añadimos una variación aleatoria en X y Z para evitar líneas rectas
        float obstacleXPosition = drone.position.x + Random.Range(-areaWidth / 2f, areaWidth / 2f); // Variación lateral
        obstacleXPosition = Mathf.Clamp(obstacleXPosition, -areaWidth / 2f, areaWidth / 2f); // Limitar en el área

        float obstacleZVariation = Random.Range(10f, 20f);  // Variar un poco en Z para dispersarlos
        obstacleZPosition += obstacleZVariation;

        // Aplicamos la posición
        newObstacle.transform.position = new Vector3(obstacleXPosition, obstacleYPosition, obstacleZPosition);

        // Asignamos el movimiento
        ObstacleMovement obstacleMovement = newObstacle.GetComponent<ObstacleMovement>();
        if (obstacleMovement == null)
        {
            obstacleMovement = newObstacle.AddComponent<ObstacleMovement>();
        }
        obstacleMovement.speed = currentObstacleSpeed;

        // Incrementar el contador de obstáculos
        currentObstacleCount++;
    }

    void SpawnErraticDrone()
    {
        if (activeDrones >= maxTotalDrones) return; // Evitar crear más drones si se ha alcanzado el máximo

        GameObject erraticDrone = Instantiate(erraticDronePrefab);

        float erraticDroneZPosition = drone.position.z + obstacleDistance + Random.Range(10f, 20f); // Generar más adelante
        float erraticDroneXPosition = drone.position.x + Random.Range(-3f, 3f); // Variación lateral menor
        float erraticDroneYPosition = Random.Range(minHeight, maxHeight);

        erraticDrone.transform.position = new Vector3(erraticDroneXPosition, erraticDroneYPosition, erraticDroneZPosition);

        ErraticDroneMovement erraticMovement = erraticDrone.GetComponent<ErraticDroneMovement>();
        if (erraticMovement != null)
        {
            erraticMovement.targetDrone = drone; // Asignar el dron principal como referencia
            erraticMovement.SetUpMovement(); // Configurar el movimiento
        }

        activeDrones++; // Incrementar el contador de drones activos
    }
}
