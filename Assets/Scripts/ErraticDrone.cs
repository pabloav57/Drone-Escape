using UnityEngine;

public class ErraticDroneMovement : MonoBehaviour
{
    public float speed = 5f;  // Velocidad del drone errático
    public Transform targetDrone;  // El dron objetivo (el dron del jugador)

    private Vector3 targetPosition;

    void Start()
    {
        // Inicializar movimiento hacia el drone objetivo
        SetUpMovement();
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
    }
}
