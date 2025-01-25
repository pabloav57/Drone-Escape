using UnityEngine;

public class DroneController : MonoBehaviour
{
    public float lateralSpeed = 5f;   // Velocidad lateral
    public float verticalSpeed = 5f;  // Velocidad vertical
    public float tiltAngle = 30f;     // Ángulo de inclinación

    private bool isColliding = false; // Para saber si el dron está colisionando
    private Rigidbody rb;

    // Control por giroscopio
    private bool useGyroscope = false;

    // Referencias para los controladores de sonido
    public SpaceSoundController spaceSoundController; // Controlador del sonido del espacio
    public MovementSoundController movementSoundController; // Controlador del sonido de las teclas ASDW

    // Referencia al GameController
    public GameController gameController;

    // Start se ejecuta antes de la primera actualización
    void Start()
    {
        isColliding = false;
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Verificar si el dispositivo soporta giroscopio
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;

            // Activar automáticamente el giroscopio si es un dispositivo móvil
            if (Application.isMobilePlatform)
            {
                useGyroscope = true;
                Debug.Log("Control por giroscopio activado automáticamente en móvil.");
            }
        }
        else
        {
            Debug.Log("El dispositivo no soporta giroscopio.");
        }
    }

    // Update se ejecuta una vez por frame
    void Update()
    {
        if (!isColliding)
        {
            if (useGyroscope && SystemInfo.supportsGyroscope)
            {
                // Control por giroscopio
                Vector3 gyroInput = Input.gyro.rotationRate; // Datos del giroscopio

                // Movimiento lateral (rotación alrededor del eje Y)
                transform.Translate(Vector3.right * gyroInput.y * lateralSpeed * Time.deltaTime);

                // Movimiento vertical (rotación alrededor del eje X)
                transform.Translate(Vector3.up * -gyroInput.x * verticalSpeed * Time.deltaTime);

                // Inclinación del dron
                float tilt = -gyroInput.y * tiltAngle;
                transform.rotation = Quaternion.Euler(0, 0, tilt);
            }
            else
            {
                // Control estándar con teclas
                float horizontalInput = Input.GetAxis("Horizontal"); // A/D o flechas izquierda/derecha
                transform.Translate(Vector3.right * horizontalInput * lateralSpeed * Time.deltaTime);

                float verticalInput = Input.GetAxis("Vertical"); // W/S o flechas arriba/abajo
                if (verticalInput != 0)
                {
                    transform.Translate(Vector3.up * verticalInput * verticalSpeed * Time.deltaTime);
                }

                float tilt = -horizontalInput * tiltAngle;
                transform.rotation = Quaternion.Euler(0, 0, tilt);

                // Reproducir sonidos de movimiento
                HandleMovementSounds();
            }
        }
        else
        {
            StopMovement();
        }
    }

    // Este método se llama cuando el dron colisiona con otro objeto
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.CompareTag("Building"))
        {
            isColliding = true;

            // Notificar al GameController que termine el juego
            gameController.EndGame();

            // Detener sonidos
            movementSoundController.StopMovementSounds();
            spaceSoundController.StopSpaceSound();

            Debug.Log($"¡Colisión con un {collision.gameObject.tag}!");
        }
    }

    // Este método se llama cuando el dron deja de colisionar con otro objeto
    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.CompareTag("Building"))
        {
            isColliding = false;
        }
    }

    // Alternar entre el control por giroscopio y teclado
    public void ToggleGyroscope()
    {
        if (SystemInfo.supportsGyroscope)
        {
            useGyroscope = !useGyroscope;
            Debug.Log(useGyroscope ? "Control por giroscopio activado." : "Control por teclado activado.");
        }
        else
        {
            Debug.Log("El dispositivo no soporta giroscopio.");
        }
    }

    // Manejo de sonidos de movimiento
    private void HandleMovementSounds()
    {
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

        if (Input.GetKeyDown(KeyCode.Space))
        {
            spaceSoundController.PlaySound(spaceSoundController.spaceSound);
        }
    }

    // Detener el movimiento y sonidos
    private void StopMovement()
    {
        movementSoundController.StopMovementSounds();
        spaceSoundController.StopSpaceSound();

        if (Input.GetAxis("Horizontal") == 0 && Input.GetAxis("Vertical") == 0)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.deltaTime * 2);
        }
    }
}
