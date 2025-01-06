using UnityEngine;
using System.Collections.Generic;

public class BuildingSpawner : MonoBehaviour
{
    public GameObject buildingPrefab; // Prefab del edificio
    public Transform drone; // Referencia al dron
    public float spawnInterval = 3f; // Intervalo entre generación de edificios
    public float spawnDistance = 100f; // Distancia de generación desde el dron
    public float buildingSpeed = 5f; // Velocidad de los edificios
    public float minHeight = 10f; // Altura mínima
    public float maxHeight = 30f; // Altura máxima
    public float minWidth = 5f; // Anchura mínima
    public float maxWidth = 15f; // Anchura máxima
    public float areaWidth = 50f; // Ancho del área de generación
    public float difficultyIncreaseInterval = 10f; // Incremento de dificultad cada cierto tiempo
    public int poolSize = 10; // Tamaño del pool de edificios
    public int buildingsInRow = 20; // Número de edificios en cada fila

    private float timeSinceLastSpawn = 0f;
    private float timeSinceStart = 0f;
    private Queue<GameObject> buildingPool = new Queue<GameObject>(); // Pool de edificios

    private List<GameObject> activeBuildings = new List<GameObject>(); // Lista de edificios activos
    private float lastSpawnZ = 0f; // Posición Z del último edificio generado

    void Start()
    {
        InitializeBuildingPool();
    }

    void Update()
    {
        timeSinceLastSpawn += Time.deltaTime;
        timeSinceStart += Time.deltaTime;

        // Incrementar dificultad con el tiempo
        if (timeSinceStart > difficultyIncreaseInterval)
        {
            buildingSpeed += 0.5f; // Incrementar velocidad
            spawnInterval = Mathf.Max(0.5f, spawnInterval - 0.1f); // Reducir intervalo mínimo
            maxHeight += 5f; // Aumentar altura máxima
            timeSinceStart = 0f; // Reiniciar contador
        }

        // Generar edificios
        if (timeSinceLastSpawn >= spawnInterval)
        {
            SpawnRowOfBuildings();
            timeSinceLastSpawn = 0f;
        }

        // Mover edificios
        MoveBuildings();
    }

    void InitializeBuildingPool()
    {
        // Inicializar el pool de edificios
        for (int i = 0; i < poolSize; i++)
        {
            GameObject building = Instantiate(buildingPrefab);
            building.SetActive(false); // Desactivar al inicio
            buildingPool.Enqueue(building);
        }
    }

    void SpawnRowOfBuildings()
    {
        // Crear una fila de edificios en la dirección del dron
        for (int i = 0; i < buildingsInRow; i++)
        {
            GameObject building;

            // Obtener un edificio del pool o crear uno nuevo si el pool está vacío
            if (buildingPool.Count > 0)
            {
                building = buildingPool.Dequeue();
                building.SetActive(true);
            }
            else
            {
                building = Instantiate(buildingPrefab);
            }

            // Configurar la posición del edificio en la fila
            float xPosition = Random.Range(-areaWidth / 2, areaWidth / 2); // Randomizar en el ancho
            float zPosition = drone.position.z + spawnDistance + (i * maxWidth); // Posición en z de la fila

            SpawnSingleBuilding(building, xPosition, zPosition);
        }

        // Actualizar la posición de la última fila generada
        lastSpawnZ += spawnDistance + (buildingsInRow * maxWidth);
    }

    void SpawnSingleBuilding(GameObject building, float xPosition, float zPosition)
    {
        // Configurar la posición y el tamaño del edificio
        float height = Random.Range(minHeight, maxHeight);
        float width = Random.Range(minWidth, maxWidth);

        Vector3 spawnPosition = new Vector3(xPosition, height / 2, zPosition);
        building.transform.position = spawnPosition;
        building.transform.localScale = new Vector3(width, height, width);

        // Añadir o ajustar BoxCollider
        BoxCollider collider = building.GetComponent<BoxCollider>() ?? building.AddComponent<BoxCollider>();
        collider.size = new Vector3(1, height, 1);
        collider.center = new Vector3(0, height / 2, 0);
        collider.isTrigger = false; // Desactivar 'Is Trigger' para una colisión física normal

        // Añadir a la lista de edificios activos
        activeBuildings.Add(building);
    }

    void MoveBuildings()
    {
        // Mover edificios activos
        for (int i = activeBuildings.Count - 1; i >= 0; i--)
        {
            GameObject building = activeBuildings[i];
            building.transform.Translate(Vector3.back * buildingSpeed * Time.deltaTime);

            // Si el edificio está fuera de la vista del dron, devolverlo al pool
            if (building.transform.position.z < drone.position.z - 10f)
            {
                activeBuildings.RemoveAt(i);
                ReturnBuildingToPool(building);
            }
        }
    }

    void ReturnBuildingToPool(GameObject building)
    {
        // Desactivar el edificio y devolverlo al pool
        building.SetActive(false);
        buildingPool.Enqueue(building);
    }
}
