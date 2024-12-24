using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject[] obstaclePrefabs;  // Array para los diferentes prefabs de obstáculos
    public float spawnInterval = 2f;  // Intervalo de tiempo entre apariciones de obstáculos
    public float obstacleDistance = 50f; // Distancia de aparición de los obstáculos
    public float minHeight = 1f; // Altura mínima para los obstáculos
    public float maxHeight = 10f; // Altura máxima para los obstáculos
    public Transform drone; // Referencia al dron
    public float obstacleSpeed = 5f; // Velocidad de los obstáculos

    private float timeSinceLastSpawn = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Controlar el tiempo entre apariciones de obstáculos
        timeSinceLastSpawn += Time.deltaTime;

        if (timeSinceLastSpawn >= spawnInterval)
        {
            SpawnObstacle();
            timeSinceLastSpawn = 0f; // Resetear el contador
        }

        // Mover los obstáculos hacia el dron (simula que avanzan hacia él)
        MoveObstacles();
    }

    void SpawnObstacle()
    {
        // Obtener la posición Z actual del dron
        float droneZPosition = drone.position.z;

        // Determinar la distancia en Z a la que aparecerá el obstáculo
        float obstacleZPosition = droneZPosition + obstacleDistance;

        // Generar una altura aleatoria en Y para el obstáculo (basada en la posición del dron)
        float obstacleYPosition = Random.Range(minHeight, maxHeight); // Altura aleatoria dentro del rango

        // Generar un desplazamiento aleatorio en X para los obstáculos (para hacer más difícil esquivar)
        float obstacleXPosition = drone.position.x + Random.Range(-5f, 5f); // Desplazamiento en X

        // Elegir un obstáculo aleatorio del array de prefabs
        int randomIndex = Random.Range(0, obstaclePrefabs.Length); // Índice aleatorio dentro del rango de prefabs
        GameObject selectedObstacle = obstaclePrefabs[randomIndex]; // Objeto seleccionado

        // Crear un nuevo obstáculo en la posición calculada
        Vector3 spawnPosition = new Vector3(obstacleXPosition, obstacleYPosition, obstacleZPosition);

        // Instanciar el obstáculo
        Instantiate(selectedObstacle, spawnPosition, Quaternion.identity);
    }

    void MoveObstacles()
    {
        // Encuentra todos los obstáculos en la escena
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle"); // Asegúrate de que los obstáculos tengan la etiqueta "Obstacle"

        foreach (GameObject obstacle in obstacles)
        {
            // Mover los obstáculos hacia el dron
            obstacle.transform.Translate(Vector3.back * obstacleSpeed * Time.deltaTime); // Mover los obstáculos hacia atrás
        }
    }
}
