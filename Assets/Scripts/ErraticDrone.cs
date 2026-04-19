using UnityEngine;

public class ErraticDroneMovement : MonoBehaviour
{
    public float speed = 5f;
    public Transform targetDrone;
    public float verticalAmplitude = 3f;
    public float verticalSpeed = 1f;

    private Vector3 targetPosition;
    private float startY;

    void Start()
    {
        startY = transform.position.y;
        SetUpMovement();
    }

    public void SetUpMovement()
    {
        if (targetDrone == null)
        {
            return;
        }

        targetPosition = targetDrone.position;
    }

    void Update()
    {
        if (targetDrone == null)
        {
            return;
        }

        targetPosition = targetDrone.position;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        float newY = startY + Mathf.Sin(Time.time * verticalSpeed) * verticalAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
