using UnityEngine;

public class Terrain : MonoBehaviour
{
        public float terrainSpeed = 5f;  // Velocidad a la que se mueve el terreno
    public Transform drone;  // Referencia al dron
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
                // Mueve el terreno hacia el dron en el eje Z (simulando que el dron se mueve hacia adelante)
        transform.Translate(Vector3.back * terrainSpeed * Time.deltaTime);
        
        // Si el terreno se mueve completamente fuera de la vista del dron, recíclalo
        if (transform.position.z < drone.position.z - 50f)
        {
            // Resetear la posición del terreno
            transform.position = new Vector3(0f, 0f, drone.position.z + 50f);
        }
        
    }
}
