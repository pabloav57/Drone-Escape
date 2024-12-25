using UnityEngine;

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

    private float timeSinceLastSpawn = 0f;
    private float timeSinceStart = 0f;

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
            SpawnBuilding();
            timeSinceLastSpawn = 0f;
        }

        // Mover edificios
        MoveBuildings();
    }

    void SpawnBuilding()
{
    float xPosition = Random.Range(-areaWidth / 2, areaWidth / 2);
    float zPosition = drone.position.z + spawnDistance;
    float height = Random.Range(minHeight, maxHeight);
    float width = Random.Range(minWidth, maxWidth);

    Vector3 spawnPosition = new Vector3(xPosition, height / 2, zPosition);

    // Instanciar y configurar edificio
    GameObject building = Instantiate(buildingPrefab, spawnPosition, Quaternion.identity);
    building.transform.localScale = new Vector3(width, height, width); // Escalar el edificio
    building.tag = "Building";

    // Añadir o ajustar BoxCollider
    BoxCollider collider = building.GetComponent<BoxCollider>() ?? building.AddComponent<BoxCollider>();
    collider.size = new Vector3(1, height, 1);
    collider.center = new Vector3(0, height / 2, 0);

    // Desactivar 'Is Trigger' para que el collider actúe como una colisión física normal
    collider.isTrigger = false;
}


    void MoveBuildings()
    {
        GameObject[] buildings = GameObject.FindGameObjectsWithTag("Building");

        foreach (GameObject building in buildings)
        {
            building.transform.Translate(Vector3.back * buildingSpeed * Time.deltaTime);

            if (building.transform.position.z < drone.position.z - 10f)
            {
                Destroy(building);
            }
        }
    }
}
