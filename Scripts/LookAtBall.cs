using UnityEngine;

public class LookAtBall : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform ball;

    [Header("Settings")]
    [SerializeField] private float minFOV;
    [SerializeField] private float maxFOV;
    [SerializeField] private float maxFOVDistance;
    [SerializeField] private float FOVLerpSpeed;
    [SerializeField] private float rotationSmoothSpeed = 5f;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        // look at the ball
        Quaternion targetRotation = Quaternion.LookRotation(ball.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);

        // adjust the FOV
        float distanceFromBall = Vector3.Distance(ball.position, transform.position);

        float targetFOV = Mathf.Lerp(maxFOV, minFOV, distanceFromBall / maxFOVDistance);

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * FOVLerpSpeed);
    }
}
