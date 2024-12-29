using UnityEngine;

public class ObstacleMovement : MonoBehaviour
{
    public float speed = 20f;  // Velocidad del movimiento
    public Transform drone;   // Referencia al dron

    void Update()
    {
        // Mover el obstáculo hacia atrás en función de la velocidad y el tiempo
        transform.Translate(Vector3.back * speed * Time.deltaTime);

        // Verificar si el obstáculo ha pasado la posición del dron y reposicionarlo
        if (transform.position.z < drone.position.z - 20f)  // Puedes ajustar este valor según lo necesites
        {
            // Reposicionar el obstáculo
            ResetObstaclePosition();
        }
    }

    public void ResetObstaclePosition()
    {
        // Puedes hacer que el obstáculo vuelva a aparecer al frente del dron
        float newZPosition = drone.position.z + 100f;  // Ajusta la distancia según necesites
        float newXPosition = Random.Range(-5f, 5f);  // Cambia el rango si lo deseas
        float newYPosition = Random.Range(1f, 10f);  // Cambia el rango si lo deseas

        transform.position = new Vector3(newXPosition, newYPosition, newZPosition);
    }
}
