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

    void Start()
    {
        // Cargar el valor de dificultad desde PlayerPrefs
        int difficultyIndex = PlayerPrefs.GetInt("SelectedDifficulty", 1); // 1 es el valor predeterminado (Normal)

        // Ajustar parámetros según la dificultad
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
        currentObstacleSpeed = Mathf.Min(10f, baseObstacleSpeed + (elapsedTime * difficultyIncreaseRate)); // Limitar la velocidad de incremento de la dificultad

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
        // Crear el pool de objetos
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)]); // Inicializar con un prefab aleatorio
            obj.SetActive(false); // Desactivar para no aparecer en la escena
            objectPool.Enqueue(obj); // Añadirlo al pool
        }
    }

    void SpawnObstacle()
    {
        GameObject newObstacle;

        // Usar un objeto del pool o crear uno nuevo si es necesario
        if (objectPool.Count > 0)
        {
            newObstacle = objectPool.Dequeue();
            newObstacle.SetActive(true); // Activar el objeto
        }
        else
        {
            // Si el pool está vacío, instanciamos un nuevo objeto
            newObstacle = Instantiate(obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)]);
        }

        // Configurar la posición de spawn
        float droneZPosition = drone.position.z;
        float obstacleZPosition = droneZPosition + obstacleDistance;
        float obstacleYPosition = Random.Range(minHeight, maxHeight);
        float obstacleXPosition = drone.position.x + Random.Range(-5f, 5f);

        Vector3 spawnPosition = new Vector3(obstacleXPosition, obstacleYPosition, obstacleZPosition);
        newObstacle.transform.position = spawnPosition;

        // Configurar la velocidad del obstáculo
        ObstacleMovement obstacleMovement = newObstacle.GetComponent<ObstacleMovement>();
        if (obstacleMovement == null)
        {
            obstacleMovement = newObstacle.AddComponent<ObstacleMovement>();
        }
        obstacleMovement.speed = currentObstacleSpeed;

        // Asignar la referencia del dron al script 'ObstacleMovement' para que el obstáculo se mueva en función del dron
        obstacleMovement.drone = drone;

        // Incrementar el contador de obstáculos
        currentObstacleCount++;
    }

    // Método para reciclar los obstáculos cuando salen de la vista
    void RecycleObstacle(GameObject obstacle)
    {
        obstacle.SetActive(false); // Desactivar el obstáculo

        // Reposicionar el obstáculo antes de reciclarlo
        obstacle.GetComponent<ObstacleMovement>().ResetObstaclePosition();

        // Asegurarse de que el dron esté correctamente asignado en el reciclaje
        obstacle.GetComponent<ObstacleMovement>().drone = drone;

        // Volver a añadir el obstáculo al pool
        objectPool.Enqueue(obstacle);
        currentObstacleCount--;
    }
}
