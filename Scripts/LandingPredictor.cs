using UnityEngine;

public class BallLandingPredictor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody ballRigidbody;
    [SerializeField] private GameObject landingIndicatorPrefab;

    [Header("Settings")]
    [SerializeField] private float timeStep = 0.1f;
    [SerializeField] private float maxSimulationTime = 10f;
    [SerializeField] private float groundDetectionHeight;
    [SerializeField] private float scaleDivider;
    [SerializeField] private float timeBeforeDisable = 0.5f;
    [SerializeField] private LayerMask groundMask;

    private GameObject landingIndicatorInstance;

    private bool isOnGround;
    private bool isTurningOff; // used to turn off the landing indicator smoothly

    private float originalScale;

    private void Start()
    {
        landingIndicatorInstance = Instantiate(landingIndicatorPrefab);
        originalScale = landingIndicatorInstance.transform.localScale.x;
    }

    private void OnDestroy()
    {
        Destroy(landingIndicatorInstance);
    }

    private void Update()
    {
        isOnGround = Physics.Raycast(transform.position, Vector3.down, groundDetectionHeight, groundMask);

        if (!isOnGround)
        {
            landingIndicatorInstance.SetActive(true);

            landingIndicatorInstance.transform.position = PredictLandingPoint(ballRigidbody.position, ballRigidbody.linearVelocity);
            HandleLandingPointScale();
        }

        else if (!isTurningOff)
        {
            isTurningOff = true;
            Invoke(nameof(DisableLandingIndictator), timeBeforeDisable);
        }
    }

    private Vector3 PredictLandingPoint(Vector3 startPosition, Vector3 velocity)
    {
        Vector3 position = startPosition;
        Vector3 currentVelocity = velocity;

        float elapsedTime = 0f;

        while (elapsedTime < maxSimulationTime)
        {
            Vector3 nextVelocity = currentVelocity + Physics.gravity * timeStep;
            Vector3 nextPosition = position + currentVelocity * timeStep;

            // Raycast to detect ground collision
            if (Physics.Raycast(position, nextPosition - position, out RaycastHit hit, (nextPosition - position).magnitude, groundMask))
                return hit.point;

            position = nextPosition;
            currentVelocity = nextVelocity;
            elapsedTime += timeStep;
        }

        // default return
        return new (position.x, -1.042328f, position.z);
    }

    private void HandleLandingPointScale()
    {
        float scaleMultiplier = Mathf.Max(Mathf.Abs(Vector3.Distance(transform.position, landingIndicatorInstance.transform.position)) / scaleDivider, 3);
        float calculatedScale = originalScale * scaleMultiplier;

        landingIndicatorInstance.transform.localScale = new(calculatedScale, landingIndicatorInstance.transform.localScale.y, calculatedScale);
    }

    private void DisableLandingIndictator()
    {
        landingIndicatorInstance.SetActive(false);
        isTurningOff = false;
    }
}
