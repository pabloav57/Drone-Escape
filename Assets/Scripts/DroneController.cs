using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DroneController : MonoBehaviour
{
    private const string ControlModeKey = "UseGyroscope";
    private const string SkinKey = "SelectedSkin";

    public float lateralSpeed = 5f;
    public float verticalSpeed = 5f;
    public float forwardSpeed = 8f;
    public float tiltAngle = 30f;
    public float tiltSmoothness = 8f;
    public float movementSmoothness = 10f;
    public float horizontalLimit = 20f;
    public float minHeight = 1.5f;
    public float maxHeight = 18f;
    public Color crashTint = new Color(1f, 0.3f, 0.3f, 1f);
    public float crashFlashDuration = 0.35f;

    private bool isColliding;
    private Rigidbody rb;
    private bool useGyroscope;
    private Vector3 smoothedVelocity;
    private Renderer[] droneRenderers;
    private CameraFollow cameraFollow;
    private Coroutine crashFlashRoutine;
    private DroneSkinController skinController;

    public SpaceSoundController spaceSoundController;
    public MovementSoundController movementSoundController;
    public GameController gameController;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;

        droneRenderers = GetComponentsInChildren<Renderer>(true);
        cameraFollow = FindAnyObjectByType<CameraFollow>();
        skinController = GetComponent<DroneSkinController>();
        useGyroscope = PlayerPrefs.GetInt(ControlModeKey, 0) == 1;

        if (skinController != null)
        {
            skinController.ApplySkin(PlayerPrefs.GetInt(SkinKey, 0));
        }

        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;

            if (Application.isMobilePlatform)
            {
                useGyroscope = true;
                PlayerPrefs.SetInt(ControlModeKey, 1);
                PlayerPrefs.Save();
            }
        }
    }

    void FixedUpdate()
    {
        if (isColliding)
        {
            StopMovement();
            return;
        }

        Vector2 input = useGyroscope && SystemInfo.supportsGyroscope ? ReadGyroscopeInput() : ReadKeyboardInput();
        MoveDrone(input);
        RotateDrone(input.x);
    }

    void Update()
    {
        if (isColliding)
        {
            return;
        }

        Vector2 audioInput = useGyroscope && SystemInfo.supportsGyroscope ? ReadGyroscopeInput() : ReadKeyboardInput();
        HandleMovementSounds(audioInput.x, audioInput.y);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.CompareTag("Building"))
        {
            if (isColliding)
            {
                return;
            }

            isColliding = true;
            movementSoundController?.StopMovementSounds();
            spaceSoundController?.StopSpaceSound();
            cameraFollow?.PlayCrashFeedback();

            if (crashFlashRoutine != null)
            {
                StopCoroutine(crashFlashRoutine);
            }

            crashFlashRoutine = StartCoroutine(CrashFlashRoutine());
            gameController?.EndGame();
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.CompareTag("Building"))
        {
            isColliding = false;
        }
    }

    public void ToggleGyroscope()
    {
        if (!SystemInfo.supportsGyroscope)
        {
            Debug.Log("El dispositivo no soporta giroscopio.");
            return;
        }

        useGyroscope = !useGyroscope;
        PlayerPrefs.SetInt(ControlModeKey, useGyroscope ? 1 : 0);
        PlayerPrefs.Save();
    }

    private Vector2 ReadKeyboardInput()
    {
        return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
    }

    private Vector2 ReadGyroscopeInput()
    {
        Vector3 gyroInput = Input.gyro.rotationRate;
        return new Vector2(gyroInput.y, -gyroInput.x);
    }

    private void MoveDrone(Vector2 input)
    {
        Vector3 targetVelocity = new Vector3(input.x * lateralSpeed, input.y * verticalSpeed, forwardSpeed);
        smoothedVelocity = Vector3.Lerp(smoothedVelocity, targetVelocity, movementSmoothness * Time.fixedDeltaTime);

        Vector3 nextPosition = rb.position + (smoothedVelocity * Time.fixedDeltaTime);
        nextPosition.x = Mathf.Clamp(nextPosition.x, -horizontalLimit, horizontalLimit);
        nextPosition.y = Mathf.Clamp(nextPosition.y, minHeight, maxHeight);

        rb.MovePosition(nextPosition);
    }

    private void RotateDrone(float horizontalInput)
    {
        float targetTilt = -horizontalInput * tiltAngle;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetTilt);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, tiltSmoothness * Time.fixedDeltaTime));
    }

    private void HandleMovementSounds(float horizontalInput, float verticalInput)
    {
        float movementIntensity = Mathf.Clamp01(new Vector2(horizontalInput, verticalInput).magnitude);
        bool boostActive = Input.GetKey(KeyCode.Space);

        if (horizontalInput < -0.1f)
        {
            movementSoundController?.PlaySound(movementSoundController.aSound);
        }
        else if (horizontalInput > 0.1f)
        {
            movementSoundController?.PlaySound(movementSoundController.dSound);
        }
        else if (verticalInput > 0.1f)
        {
            movementSoundController?.PlaySound(movementSoundController.wSound);
        }
        else if (verticalInput < -0.1f)
        {
            movementSoundController?.PlaySound(movementSoundController.sSound);
        }
        else
        {
            movementSoundController?.StopMovementSounds();
        }

        movementSoundController?.SetMovementState(movementIntensity, boostActive);
        spaceSoundController?.SetBoostActive(boostActive);
    }

    private void StopMovement()
    {
        smoothedVelocity = Vector3.Lerp(smoothedVelocity, Vector3.zero, Time.fixedDeltaTime * 4f);
        rb.linearVelocity = Vector3.zero;
        movementSoundController?.StopMovementSounds();
        spaceSoundController?.StopSpaceSound();
    }

    private IEnumerator CrashFlashRoutine()
    {
        Color[] originalColors = new Color[droneRenderers.Length];

        for (int i = 0; i < droneRenderers.Length; i++)
        {
            if (droneRenderers[i] != null && droneRenderers[i].material.HasProperty("_Color"))
            {
                originalColors[i] = droneRenderers[i].material.color;
                droneRenderers[i].material.color = crashTint;
            }
        }

        yield return new WaitForSecondsRealtime(crashFlashDuration);

        for (int i = 0; i < droneRenderers.Length; i++)
        {
            if (droneRenderers[i] != null && droneRenderers[i].material.HasProperty("_Color"))
            {
                droneRenderers[i].material.color = originalColors[i];
            }
        }

        crashFlashRoutine = null;
    }
}

public class DroneSkinController : MonoBehaviour
{
    private static readonly Color[] SkinColors =
    {
        new Color(1f, 1f, 1f),
        new Color(1f, 0.58f, 0.18f),
        new Color(0.34f, 0.88f, 0.42f),
        new Color(0.28f, 0.62f, 1f),
        new Color(0.2f, 0.2f, 0.22f),
        new Color(0.92f, 0.92f, 0.92f)
    };

    [Range(0.35f, 1.5f)]
    public float tintStrength = 0.85f;

    private Renderer[] cachedRenderers;
    private Color[] baseColors;

    void Awake()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        baseColors = new Color[cachedRenderers.Length];

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] != null && cachedRenderers[i].material.HasProperty("_Color"))
            {
                baseColors[i] = cachedRenderers[i].material.color;
            }
        }
    }

    public void ApplySkin(int skinIndex)
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            return;
        }

        skinIndex = Mathf.Clamp(skinIndex, 0, SkinColors.Length - 1);
        Color targetTint = SkinColors[skinIndex];

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] == null || !cachedRenderers[i].material.HasProperty("_Color"))
            {
                continue;
            }

            Color baseColor = baseColors[i] == default ? Color.white : baseColors[i];
            cachedRenderers[i].material.color = Color.Lerp(baseColor, targetTint, tintStrength);
        }
    }
}
