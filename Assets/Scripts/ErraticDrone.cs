using UnityEngine;

public class ErraticDroneMovement : MonoBehaviour
{
    public float speed = 5f;
    public Transform targetDrone;
    public float verticalAmplitude = 3f;
    public float verticalSpeed = 1f;
    public bool destabilizesTarget = true;
    public float destabilizeRadius = 8f;
    public float destabilizeStrength = 0.42f;
    public float destabilizeCooldown = 0.35f;
    public float sideOffset = 4f;
    public float followDistance = 8f;
    public float pursuitDuration = 10f;
    public float retreatSpeedMultiplier = 1.6f;

    private Vector3 targetPosition;
    private float startY;
    private float pursuitStartTime;
    private float nextDestabilizeTime;
    private DroneController targetController;

    public bool IsThreatActive => targetDrone != null && Time.time - pursuitStartTime < pursuitDuration;
    public bool ShouldShowAlert => IsThreatActive && targetDrone != null && Vector3.Distance(transform.position, targetDrone.position) <= destabilizeRadius * 2.2f;
    public float ThreatRemainingTime => Mathf.Max(0f, pursuitDuration - (Time.time - pursuitStartTime));

    void Start()
    {
        startY = transform.position.y;
        pursuitStartTime = Time.time;
        ConfigureAsHazard();
        SetUpMovement();
    }

    public void SetUpMovement()
    {
        if (targetDrone == null)
        {
            return;
        }

        targetController = targetDrone.GetComponent<DroneController>();
        targetPosition = GetPursuitPosition();
    }

    private void ConfigureAsHazard()
    {
        gameObject.tag = "Obstacle";

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }

        body.isKinematic = true;
        body.useGravity = false;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        if (colliders.Length == 0)
        {
            SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.radius = 1.6f;
            colliders = new Collider[] { sphereCollider };
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].isTrigger = true;
            }
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            Material[] materials = renderers[i].materials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                ApplyWarningColor(materials[materialIndex]);
            }
        }
    }

    private void ApplyWarningColor(Material material)
    {
        if (material == null)
        {
            return;
        }

        Color warningColor = new Color(1f, 0.18f, 0.08f, 1f);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", warningColor);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", warningColor);
        }
    }

    void Update()
    {
        if (targetDrone == null)
        {
            return;
        }

        if (IsThreatActive)
        {
            targetPosition = GetPursuitPosition();
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        }
        else
        {
            transform.Translate(Vector3.back * speed * retreatSpeedMultiplier * Time.deltaTime, Space.World);
        }

        float newY = startY + Mathf.Sin(Time.time * verticalSpeed) * verticalAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        if (IsThreatActive)
        {
            TryDestabilizeTarget();
        }
        else if (transform.position.z < targetDrone.position.z - 45f)
        {
            Destroy(gameObject);
        }
    }

    private Vector3 GetPursuitPosition()
    {
        float side = Mathf.Sin(Time.time * 1.3f) * sideOffset;
        return targetDrone.position + new Vector3(side, 0f, followDistance);
    }

    private void TryDestabilizeTarget()
    {
        if (!destabilizesTarget || targetController == null || Time.time < nextDestabilizeTime)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, targetDrone.position);
        if (distance > destabilizeRadius)
        {
            return;
        }

        Vector3 direction = (targetDrone.position - transform.position).normalized;
        float sideImpulse = Mathf.Sign(direction.x == 0f ? Mathf.Sin(Time.time) : direction.x) * destabilizeStrength;
        float verticalImpulse = Mathf.Sin(Time.time * 4f) * destabilizeStrength * 0.45f;

        targetController.ApplyInstability(new Vector2(sideImpulse, verticalImpulse));
        nextDestabilizeTime = Time.time + destabilizeCooldown;
    }
}
