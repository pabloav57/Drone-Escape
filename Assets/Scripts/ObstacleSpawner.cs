using UnityEngine;

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

    private float timeSinceLastSpawn = 0f;
    private float currentSpawnInterval;
    private float currentObstacleSpeed;
    private float elapsedTime = 0f;

    void Start()
    {
        currentSpawnInterval = baseSpawnInterval;
        currentObstacleSpeed = baseObstacleSpeed;
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;

        // Incrementar dificultad con el tiempo
        currentSpawnInterval = Mathf.Max(0.5f, baseSpawnInterval - (elapsedTime * difficultyIncreaseRate));
        currentObstacleSpeed = baseObstacleSpeed + (elapsedTime * difficultyIncreaseRate);

        // Controlar el tiempo entre spawns
        timeSinceLastSpawn += Time.deltaTime;
        if (timeSinceLastSpawn >= currentSpawnInterval)
        {
            SpawnObstacle();
            timeSinceLastSpawn = 0f;
        }
    }

    void SpawnObstacle()
    {
        float droneZPosition = drone.position.z;
        float obstacleZPosition = droneZPosition + obstacleDistance;
        float obstacleYPosition = Random.Range(minHeight, maxHeight);
        float obstacleXPosition = drone.position.x + Random.Range(-5f, 5f);

        int randomIndex = Random.Range(0, obstaclePrefabs.Length);
        GameObject selectedObstacle = obstaclePrefabs[randomIndex];
        Vector3 spawnPosition = new Vector3(obstacleXPosition, obstacleYPosition, obstacleZPosition);

        GameObject newObstacle = Instantiate(selectedObstacle, spawnPosition, Quaternion.identity);

        // Añadir movimiento a los obstáculos
        ObstacleMovement obstacleMovement = newObstacle.AddComponent<ObstacleMovement>();
        obstacleMovement.speed = currentObstacleSpeed;
        obstacleMovement.drone = drone;
    }
}

public class ObstacleMovement : MonoBehaviour
{
    public float speed;
    public Transform drone;

    void Update()
    {
        // Mover hacia el dron
        transform.Translate(Vector3.back * speed * Time.deltaTime);

        // Destruir el obstáculo si está fuera del área de juego
        if (transform.position.z < drone.position.z - 10f)
        {
            Destroy(gameObject);
        }
    }

    void OnColisionEnter(Collider other)
    {
        // Si colisiona con el dron, no lo destruyas automáticamente
        if (other.CompareTag("Drone"))
        {
            Debug.Log("Obstacle passed through the drone.");
        }
    }
}
