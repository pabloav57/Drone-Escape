using UnityEngine;

public class ErraticDroneMovement : MonoBehaviour
{
    public float speed = 5f;  // Velocidad del drone errático
    public Transform targetDrone;  // El dron objetivo (el dron del jugador)
    public float verticalAmplitude = 3f;  // Amplitud del movimiento vertical
    public float verticalSpeed = 1f;  // Velocidad del movimiento vertical

    private Vector3 targetPosition;
    private float startY;

    void Start()
    {
        // Inicializar movimiento hacia el drone objetivo
        SetUpMovement();
        startY = transform.position.y;  // Guardar la posición inicial en el eje Y
    }

    public void SetUpMovement()
    {
        // Establecer la posición objetivo al drone del jugador
        targetPosition = targetDrone.position;  // El drone errático se moverá hacia el drone del jugador
    }

    void Update()
    {
        // Mover el dron errático hacia el objetivo
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        // Si ha llegado a la posición del dron objetivo, reestablecer el movimiento
        if (transform.position == targetPosition)
        {
            SetUpMovement(); // Actualizar la posición objetivo (puedes hacerlo si lo deseas para hacerlo más impredecible)
        }

        // Controlar el movimiento vertical
        float newY = startY + Mathf.Sin(Time.time * verticalSpeed) * verticalAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
