using UnityEngine;

public class ObstacleMovement : MonoBehaviour
{
    public float speed = 20f;
    public Transform drone;
    public float lateralSpeed = 2f;
    public float verticalSpeed = 2f;
    public float forwardSpeed = 2f;
    public int scoreValue = 1;
    public GameController gameController;
    public ObstacleSpawner obstacleSpawner;

    private float randomMovementFactorX;
    private float randomMovementFactorY;
    private float randomMovementFactorZ;
    private float lateralAmplitude = 1.5f;
    private float verticalAmplitude = 1.2f;
    private float forwardAmplitude = 0.4f;
    private float phaseOffset;
    private int movementPattern;
    private bool hasScored;

    void Start()
    {
        ResolveDroneReference();
        ResolveGameControllerReference();
        RandomizeMovement();
    }

    void OnEnable()
    {
        hasScored = false;
        RandomizeMovement();
    }

    void Update()
    {
        if (drone == null)
        {
            ResolveDroneReference();
            if (drone == null)
            {
                return;
            }
        }

        float movementSpeed = obstacleSpawner != null ? obstacleSpawner.CurrentObstacleSpeed : speed;
        transform.Translate(Vector3.back * movementSpeed * Time.deltaTime);

        float time = Time.time + phaseOffset;
        float lateralMovement = GetLateralMovement(time);
        transform.Translate(Vector3.right * lateralMovement * Time.deltaTime);

        float verticalMovement = GetVerticalMovement(time);
        transform.Translate(Vector3.up * verticalMovement * Time.deltaTime);

        float forwardMovement = Mathf.Sin(time * forwardSpeed) * randomMovementFactorZ * forwardAmplitude;
        transform.Translate(Vector3.forward * forwardMovement * Time.deltaTime);

        if (obstacleSpawner != null)
        {
            Vector3 clampedPosition = transform.position;
            clampedPosition.x = obstacleSpawner.ClampObstacleX(clampedPosition.x);
            transform.position = clampedPosition;
        }

        if (!hasScored && transform.position.z < drone.position.z)
        {
            hasScored = true;
            ResolveGameControllerReference();
            gameController?.AddObstaclePoint(scoreValue);
        }

        if (transform.position.z < drone.position.z - 20f)
        {
            ResetObstaclePosition();
        }
    }

    public void ResetObstaclePosition()
    {
        if (drone == null)
        {
            return;
        }

        if (obstacleSpawner != null)
        {
            transform.position = obstacleSpawner.GetObstacleSpawnPosition(80f, 120f);
        }
        else
        {
            float newZPosition = drone.position.z + 100f;
            float newXPosition = drone.position.x + Random.Range(-5f, 5f);
            float newYPosition = Random.Range(Mathf.Max(1f, drone.position.y - 3f), drone.position.y + 3f);

            transform.position = new Vector3(newXPosition, newYPosition, newZPosition);
        }

        hasScored = false;
        RandomizeMovement();
    }

    public void ConfigureMovementPattern(float difficultyProgress)
    {
        movementPattern = Random.Range(0, difficultyProgress > 0.35f ? 4 : 3);
        lateralAmplitude = Mathf.Lerp(1.2f, 4.2f, difficultyProgress);
        verticalAmplitude = Mathf.Lerp(0.9f, 3.6f, difficultyProgress);
        forwardAmplitude = Mathf.Lerp(0.2f, 1.1f, difficultyProgress);
        lateralSpeed = Mathf.Lerp(1.2f, 3.4f, difficultyProgress);
        verticalSpeed = Mathf.Lerp(1.1f, 3.2f, difficultyProgress);
        forwardSpeed = Mathf.Lerp(0.8f, 2.2f, difficultyProgress);
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void ResolveDroneReference()
    {
        if (drone != null)
        {
            return;
        }

        GameObject droneObject = GameObject.FindGameObjectWithTag("Drone");
        if (droneObject != null)
        {
            drone = droneObject.transform;
        }
        else
        {
            Debug.LogError("No se encontro un objeto con el tag 'Drone'.");
        }
    }

    private void ResolveGameControllerReference()
    {
        if (gameController != null)
        {
            return;
        }

        gameController = FindAnyObjectByType<GameController>();
    }

    private void RandomizeMovement()
    {
        randomMovementFactorX = Random.Range(-1f, 1f);
        randomMovementFactorY = Random.Range(-1f, 1f);
        randomMovementFactorZ = Random.Range(-1f, 1f);

        if (obstacleSpawner != null)
        {
            ConfigureMovementPattern(obstacleSpawner.DifficultyProgress);
        }
    }

    private float GetLateralMovement(float time)
    {
        switch (movementPattern)
        {
            case 1:
                return Mathf.Sin(time * lateralSpeed) * lateralAmplitude * Mathf.Sign(randomMovementFactorX == 0f ? 1f : randomMovementFactorX);
            case 3:
                return Mathf.Sin(time * lateralSpeed * 1.4f) * lateralAmplitude * 0.65f;
            default:
                return Mathf.Sin(time * lateralSpeed) * randomMovementFactorX * lateralAmplitude;
        }
    }

    private float GetVerticalMovement(float time)
    {
        switch (movementPattern)
        {
            case 0:
                return Mathf.Cos(time * verticalSpeed) * verticalAmplitude * Mathf.Sign(randomMovementFactorY == 0f ? 1f : randomMovementFactorY);
            case 2:
                return Mathf.Sin(time * verticalSpeed) * verticalAmplitude;
            case 3:
                return Mathf.Cos(time * verticalSpeed * 1.25f) * verticalAmplitude * 0.8f;
            default:
                return Mathf.Cos(time * verticalSpeed) * randomMovementFactorY * verticalAmplitude;
        }
    }
}
