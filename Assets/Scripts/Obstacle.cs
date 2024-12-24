using UnityEngine;

public class Obs : MonoBehaviour
{
    public float speed = 15f; // Velocidad del movimiento
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Mover el obstáculo hacia el dron
        transform.Translate(Vector3.back * speed * Time.deltaTime);

        // Destruir el obstáculo si pasa al dron
        if (transform.position.z < -10)
        {
            Destroy(gameObject);
        }
    }
}
