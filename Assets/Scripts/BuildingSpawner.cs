using UnityEngine;

public class BuildingSpawner : MonoBehaviour
{
    public GameObject buildingPrefab; // Prefab del edificio
    public Transform drone; // Referencia al dron
    public float spawnInterval = 3f; // Intervalo de tiempo entre generación de edificios
    public float spawnDistance = 100f; // Distancia inicial de los edificios al dron
    public float buildingSpeed = 5f; // Velocidad a la que los edificios se mueven
    public float minHeight = 10f; // Altura mínima de los edificios
    public float maxHeight = 30f; // Altura máxima de los edificios
    public float areaWidth = 50f; // Anchura del área donde se generan edificios

    private float timeSinceLastSpawn = 0f;

    void Update()
    {
        // Controlar el tiempo entre generación de edificios
        timeSinceLastSpawn += Time.deltaTime;

        if (timeSinceLastSpawn >= spawnInterval)
        {
            SpawnBuilding();
            timeSinceLastSpawn = 0f; // Resetear el contador
        }

        // Mover los edificios hacia el dron
        MoveBuildings();
    }

void SpawnBuilding()
{
    // Calcular una posición aleatoria dentro del área de generación
    float xPosition = Random.Range(-areaWidth / 2, areaWidth / 2);
    float zPosition = drone.position.z + spawnDistance;

    // Generar una altura aleatoria para el edificio
    float height = Random.Range(minHeight, maxHeight);

    // Crear la posición de aparición
    Vector3 spawnPosition = new Vector3(xPosition, height / 2, zPosition);

    // Instanciar el edificio
    GameObject building = Instantiate(buildingPrefab, spawnPosition, Quaternion.identity);

    // Ajustar la escala del edificio según su altura
    building.transform.localScale = new Vector3(1, height, 1);

    // Añadir una etiqueta para identificar los edificios
    building.tag = "Building";

    // Verificar y añadir un BoxCollider si no tiene uno
    if (building.GetComponent<BoxCollider>() == null)
    {
        BoxCollider collider = building.AddComponent<BoxCollider>();
        
        // Ajustar el tamaño del collider para que coincida con la escala del edificio
        collider.size = new Vector3(1, height, 1);
        collider.center = new Vector3(0, height / 2, 0); // Centrar el collider en el edificio
    }
}

    void MoveBuildings()
    {
        // Buscar todos los edificios generados con la etiqueta "Building"
        GameObject[] buildings = GameObject.FindGameObjectsWithTag("Building");

        foreach (GameObject building in buildings)
        {
            // Mover el edificio hacia el dron
            building.transform.Translate(Vector3.back * buildingSpeed * Time.deltaTime);

            // Destruir el edificio si pasa al dron
            if (building.transform.position.z < drone.position.z - 10f)
            {
                Destroy(building);
            }
        }
    }
}
