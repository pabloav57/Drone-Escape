using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform drone; // Referencia al dron
    public Vector3 offset;  // Distancia de la cámara al dron

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update(){
            // Ajustar la posición de la cámara para seguir al dron
        transform.position = drone.position + offset;
    }
}
