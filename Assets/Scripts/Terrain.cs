using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public int width = 512;  // Ancho del terreno
    public int height = 512; // Altura del terreno
    public float detail = 10f;  // Detalle de la textura
    public Transform player;  // Referencia al jugador
    public GameObject planePrefab;  // Prefab de Plane para el terreno
    public float planeScale = 2f;  // Escala del Plane

    void Start()
    {
        // Crear el terreno
        Terrain terrain = Terrain.CreateTerrainGameObject(new TerrainData()).GetComponent<Terrain>();
        
        // Configurar el tamaño del terreno
        terrain.terrainData = GenerateTerrainData(terrain.terrainData);

        // Asegurarse de que el terreno tenga un Terrain Collider
        if (terrain.gameObject.GetComponent<TerrainCollider>() == null)
        {
            TerrainCollider terrainCollider = terrain.gameObject.AddComponent<TerrainCollider>();
            terrainCollider.terrainData = terrain.terrainData;  // Asignar la data del terreno al collider
        }

        // Añadir los planes sobre el terreno
        AddPlanesToTerrain(terrain);
    }

    TerrainData GenerateTerrainData(TerrainData terrainData)
    {
        terrainData.heightmapResolution = Mathf.Max(width, height) + 1;
        terrainData.SetHeights(0, 0, GenerateHeights());
        return terrainData;
    }

    float[,] GenerateHeights()
    {
        float[,] heights = new float[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                heights[x, y] = Mathf.PerlinNoise(x / detail, y / detail);
            }
        }
        return heights;
    }

    void AddPlanesToTerrain(Terrain terrain)
    {
        if (planePrefab == null)
        {
            Debug.LogError("El prefab del plane no está asignado.");
            return;
        }

        for (int x = 0; x < width; x += (int)planeScale)
        {
            for (int y = 0; y < height; y += (int)planeScale)
            {
                float terrainHeight = terrain.terrainData.GetHeight(x, y);
                Vector3 spawnPosition = new Vector3(x, terrainHeight, y);
                GameObject planeInstance = Instantiate(planePrefab, spawnPosition, Quaternion.identity);
                planeInstance.transform.localScale = new Vector3(planeScale, 1, planeScale);
            }
        }
    }
}
