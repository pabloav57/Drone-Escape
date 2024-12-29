using UnityEngine;

public class ObstacleMovement : MonoBehaviour
{
    public float speed = 20f;  // Velocidad del movimiento
    public Transform drone;    // Referencia al dron

    // Parámetros de movimiento aleatorio
    public float lateralSpeed = 2f;   // Velocidad lateral (X)
    public float verticalSpeed = 2f;  // Velocidad vertical (Y)
    public float forwardSpeed = 2f;   // Velocidad en el eje Z (adelante/atrás)
    
    private float randomMovementFactorX;  // Factor aleatorio en X
    private float randomMovementFactorY;  // Factor aleatorio en Y
    private float randomMovementFactorZ;  // Factor aleatorio en Z

    void Start()
    {
        // Asignamos un factor aleatorio para cada eje
        randomMovementFactorX = Random.Range(-1f, 1f);
        randomMovementFactorY = Random.Range(-1f, 1f);
        randomMovementFactorZ = Random.Range(-1f, 1f);
    }

    void Update()
    {
        // Movimiento hacia atrás en el eje Z
        transform.Translate(Vector3.back * speed * Time.deltaTime);

        // Movimiento lateral (izquierda/derecha)
        float lateralMovement = Mathf.Sin(Time.time * lateralSpeed) * randomMovementFactorX;  // Movimiento lateral
        transform.Translate(Vector3.right * lateralMovement * Time.deltaTime);

        // Movimiento vertical (arriba/abajo)
        float verticalMovement = Mathf.Cos(Time.time * verticalSpeed) * randomMovementFactorY;  // Movimiento vertical
        transform.Translate(Vector3.up * verticalMovement * Time.deltaTime);

        // Movimiento hacia adelante/atrás (en el eje Z)
        float forwardMovement = Mathf.Sin(Time.time * forwardSpeed) * randomMovementFactorZ;  // Movimiento hacia adelante/atrás
        transform.Translate(Vector3.forward * forwardMovement * Time.deltaTime);

        // Reposicionar si el obstáculo ha pasado el dron
        if (transform.position.z < drone.position.z - 20f)
        {
            ResetObstaclePosition();
        }
    }

    public void ResetObstaclePosition()
    {
        // Reposicionar el obstáculo al frente del dron
        float newZPosition = drone.position.z + 100f;
        float newXPosition = Random.Range(-5f, 5f);  // Posición lateral aleatoria
        float newYPosition = Random.Range(1f, 10f);  // Posición vertical aleatoria

        transform.position = new Vector3(newXPosition, newYPosition, newZPosition);
    }
}
