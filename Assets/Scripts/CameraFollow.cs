using System.Collections;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform drone;
    public Vector3 offset = new Vector3(0f, 4f, -10f);
    public float positionSmoothTime = 0.2f;
    public float rotationSmoothSpeed = 6f;
    public bool followRotation = false;
    public float crashShakeDuration = 0.4f;
    public float crashShakeMagnitude = 0.35f;
    public float crashFovKick = 10f;

    private Vector3 velocity;
    private Vector3 shakeOffset;
    private Camera cachedCamera;
    private float baseFieldOfView;
    private Coroutine crashRoutine;

    void Start()
    {
        cachedCamera = GetComponent<Camera>();
        if (cachedCamera != null)
        {
            baseFieldOfView = cachedCamera.fieldOfView;
        }
    }

    void LateUpdate()
    {
        if (drone == null)
        {
            return;
        }

        Vector3 desiredPosition = drone.position + offset + shakeOffset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, positionSmoothTime);

        if (followRotation)
        {
            Quaternion targetRotation = Quaternion.LookRotation(drone.position - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
        }
    }

    public void PlayCrashFeedback()
    {
        if (crashRoutine != null)
        {
            StopCoroutine(crashRoutine);
        }

        crashRoutine = StartCoroutine(CrashFeedbackRoutine());
    }

    private IEnumerator CrashFeedbackRoutine()
    {
        float elapsed = 0f;

        while (elapsed < crashShakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / crashShakeDuration);
            float damper = 1f - progress;

            shakeOffset = Random.insideUnitSphere * crashShakeMagnitude * damper;
            shakeOffset.z = 0f;

            if (cachedCamera != null)
            {
                cachedCamera.fieldOfView = baseFieldOfView + (crashFovKick * damper);
            }

            yield return null;
        }

        shakeOffset = Vector3.zero;
        if (cachedCamera != null)
        {
            cachedCamera.fieldOfView = baseFieldOfView;
        }

        crashRoutine = null;
    }
}
