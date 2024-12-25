using UnityEngine;

public class InfiniteTerrain : MonoBehaviour
{
    public GameObject terrainPrefab;  // Prefab del terreno
    public int numberOfSegments = 3;  // Número de segmentos activos
    public float segmentLength = 50f;  // Longitud de cada segmento
    public float terrainSpeed = 5f;  // Velocidad del terreno
    public Transform drone;  // Referencia al dron

    private GameObject[] terrainSegments;  // Array de segmentos del terreno
    private int lastSegmentIndex = 0;  // Índice del último segmento generado

    void Start()
    {
        // Inicializar el array de segmentos
        terrainSegments = new GameObject[numberOfSegments];

        // Crear segmentos iniciales
        for (int i = 0; i < numberOfSegments; i++)
        {
            Vector3 position = new Vector3(0, 0, i * segmentLength);
            terrainSegments[i] = Instantiate(terrainPrefab, position, Quaternion.identity);
        }
    }

    void Update()
    {
        // Mover todos los segmentos hacia atrás
        foreach (GameObject segment in terrainSegments)
        {
            segment.transform.Translate(Vector3.back * terrainSpeed * Time.deltaTime);
        }

        // Verificar si el segmento más atrasado está fuera de la vista del dron
        GameObject lastSegment = terrainSegments[lastSegmentIndex];
        if (lastSegment.transform.position.z < drone.position.z - segmentLength)
        {
            RecycleSegment(lastSegment);
        }
    }

    void RecycleSegment(GameObject segment)
    {
        // Mover el segmento más atrasado al frente
        float newZPosition = terrainSegments[(lastSegmentIndex + numberOfSegments - 1) % numberOfSegments].transform.position.z + segmentLength;
        segment.transform.position = new Vector3(0, 0, newZPosition);

        // Actualizar el índice del último segmento
        lastSegmentIndex = (lastSegmentIndex + 1) % numberOfSegments;
    }
}
