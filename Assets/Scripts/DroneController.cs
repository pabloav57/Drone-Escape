using UnityEngine;

public class DroneController : MonoBehaviour
{
    public float lateralSpeed = 5f;   // Velocidad lateral
    public float verticalSpeed = 5f;  // Velocidad vertical
    public float tiltAngle = 30f;     // Ángulo de inclinación

    private bool isColliding = false; // Para saber si el dron está colisionando
    private Rigidbody rb;

    // Referencias para los controladores de sonido
    public SpaceSoundController spaceSoundController; // Controlador del sonido del espacio
    public MovementSoundController movementSoundController; // Controlador del sonido de las teclas ASDW

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
        // Movimiento lateral (izquierda/derecha) con inclinación
        float horizontalInput = Input.GetAxis("Horizontal"); // A/D o flechas izquierda/derecha
        transform.Translate(Vector3.right * horizontalInput * lateralSpeed * Time.deltaTime);

        // Inclinarse al moverse lateralmente
        float tilt = -horizontalInput * tiltAngle;
        transform.rotation = Quaternion.Euler(0, 0, tilt);

        // Movimiento vertical (arriba/abajo)
        float verticalInput = Input.GetAxis("Vertical"); // W/S o flechas arriba/abajo
        
        // Solo mover en vertical si hay entrada, de lo contrario el dron mantiene su altura.
        if (verticalInput != 0)
        {
            transform.Translate(Vector3.up * verticalInput * verticalSpeed * Time.deltaTime);
        }

        // Reproducir sonidos al presionar las teclas ASDW
        if (Input.GetKey(KeyCode.A))
        {
            movementSoundController.PlaySound(movementSoundController.aSound);
        }
        if (Input.GetKey(KeyCode.S))
        {
            movementSoundController.PlaySound(movementSoundController.sSound);
        }
        if (Input.GetKey(KeyCode.W))
        {
            movementSoundController.PlaySound(movementSoundController.wSound);
        }
        if (Input.GetKey(KeyCode.D))
        {
            movementSoundController.PlaySound(movementSoundController.dSound);
        }

        // Reproducir sonido de la tecla Espacio si se presiona
        if (Input.GetKeyDown(KeyCode.Space))
        {
            spaceSoundController.PlaySound(spaceSoundController.spaceSound);
        }
    }
    else
    {
        // Si el dron está colisionando, detener los sonidos y el movimiento
        movementSoundController.StopMovementSounds();
        spaceSoundController.StopSpaceSound();

        // Detener movimiento gradual si no se presionan teclas
        if (Input.GetAxis("Horizontal") == 0 && Input.GetAxis("Vertical") == 0)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.deltaTime * 2);
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

            // Detener todos los sonidos
            movementSoundController.StopMovementSounds();
            spaceSoundController.StopSpaceSound();

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
