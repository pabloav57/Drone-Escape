using UnityEngine;

public class ObstacleMovement : MonoBehaviour
{
    public float speed = 20f;
    public Transform drone;
    public float lateralSpeed = 2f;
    public float verticalSpeed = 2f;
    public float forwardSpeed = 2f;

    private float randomMovementFactorX;
    private float randomMovementFactorY;
    private float randomMovementFactorZ;

    void Start()
    {
        ResolveDroneReference();
        RandomizeMovement();
    }

    void OnEnable()
    {
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

        transform.Translate(Vector3.back * speed * Time.deltaTime);

        float lateralMovement = Mathf.Sin(Time.time * lateralSpeed) * randomMovementFactorX;
        transform.Translate(Vector3.right * lateralMovement * Time.deltaTime);

        float verticalMovement = Mathf.Cos(Time.time * verticalSpeed) * randomMovementFactorY;
        transform.Translate(Vector3.up * verticalMovement * Time.deltaTime);

        float forwardMovement = Mathf.Sin(Time.time * forwardSpeed) * randomMovementFactorZ;
        transform.Translate(Vector3.forward * forwardMovement * Time.deltaTime);

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

        float newZPosition = drone.position.z + 100f;
        float newXPosition = drone.position.x + Random.Range(-5f, 5f);
        float newYPosition = Random.Range(1f, 10f);

        transform.position = new Vector3(newXPosition, newYPosition, newZPosition);
        RandomizeMovement();
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

    private void RandomizeMovement()
    {
        randomMovementFactorX = Random.Range(-1f, 1f);
        randomMovementFactorY = Random.Range(-1f, 1f);
        randomMovementFactorZ = Random.Range(-1f, 1f);
    }
}
