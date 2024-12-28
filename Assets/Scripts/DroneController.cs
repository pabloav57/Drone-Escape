using UnityEngine;

public class DroneController : MonoBehaviour
{
    public float lateralSpeed = 5f;   // Velocidad lateral
    public float verticalSpeed = 5f;  // Velocidad vertical
    public float tiltAngle = 30f;     // Ángulo de inclinación

    private bool isColliding = false; // Para saber si el dron está colisionando
    private Rigidbody rb;

    // Referencia al GameController
    public GameController gameController;

    // Start se ejecuta antes de la primera actualización
    void Start()
    {
        // Nos aseguramos de que no haya colisiones al inicio
        isColliding = false;
        rb = GetComponent<Rigidbody>(); // Obtener el Rigidbody para su control

        // Verificar que el Rigidbody no esté marcado como Kinematic
        rb.isKinematic = false;

        // Cambiar el modo de detección de colisiones a "Continuous"
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    // Update se ejecuta una vez por frame
    void Update()
    {
        // Si el dron no está colisionando, puede moverse
        if (!isColliding)
        {
            // Mover lateralmente (izquierda/derecha) con inclinación
            float horizontalInput = Input.GetAxis("Horizontal"); // A/D o flechas izquierda/derecha
            transform.Translate(Vector3.right * horizontalInput * lateralSpeed * Time.deltaTime);

            // Inclinarse al moverse lateralmente
            float tilt = -horizontalInput * tiltAngle;
            transform.rotation = Quaternion.Euler(0, 0, tilt);

            // Mover verticalmente (arriba/abajo)
            float verticalInput = Input.GetAxis("Vertical"); // W/S o flechas arriba/abajo
            transform.Translate(Vector3.up * verticalInput * verticalSpeed * Time.deltaTime);
        }
        else
        {
            // Si el dron está colisionando, asegurarse de que las colisiones se mantengan activas
            // Detener cualquier movimiento si no se presionan teclas, pero no forzar el Rigidbody a ser estático
            if (Input.GetAxis("Horizontal") == 0 && Input.GetAxis("Vertical") == 0)
            {
                // Cuando no se presionan teclas, reducimos la velocidad a cero gradualmente
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.deltaTime * 2);  // Reducimos velocidad gradualmente
            }
        }
    }

    // Este método se llama cuando el dron colisiona con otro objeto
    void OnCollisionEnter(Collision collision)
    {
        // Verificar si la colisión es con un obstáculo o un edificio
        if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.CompareTag("Building"))
        {
            // Indicar que el dron está colisionando
            isColliding = true;

            // Notificar al GameController que termine el juego
            gameController.EndGame();

            // Mostrar un mensaje dependiendo del tipo de objeto con el que colisionó
            if (collision.gameObject.CompareTag("Obstacle"))
            {
                Debug.Log("¡Colisión con un obstáculo!");
            }
            else if (collision.gameObject.CompareTag("Building"))
            {
                Debug.Log("¡Colisión con un edificio!");
            }
        }
    }

    // Este método se llama cuando el dron deja de colisionar con otro objeto
    void OnCollisionExit(Collision collision)
    {
        // Verificar si la colisión es con un obstáculo o un edificio
        if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.CompareTag("Building"))
        {
            // Indicar que el dron ya no está colisionando
            isColliding = false;
        }
    }
}
