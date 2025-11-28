using UnityEngine;

public class DeterministicPhysicsSetup : MonoBehaviour
{
    [Header("Physics Timestep")]
    [SerializeField] private float fixedTimestep = 0.02f;

    [Header("Solver Iterations")]
    [Tooltip("Higher values = more accurate but slower physics")]
    [SerializeField] private int defaultSolverIterations = 8;
    [SerializeField] private int defaultSolverVelocityIterations = 2;

    [Header("Collision & Contact")]
    [SerializeField] private float defaultContactOffset = 0.01f;
    [SerializeField] private float bounceThreshold = 2f;
    [SerializeField] private float sleepThreshold = 0.005f;

    [Header("Advanced Settings")]
    [SerializeField] private bool useEnhancedDeterminism = true;
    [SerializeField] private bool reuseCollisionCallbacks = true;
    [SerializeField] private int defaultMaxAngularSpeed = 50;

    [Header("Auto Configuration")]
    [SerializeField] private bool configureOnAwake = true;

    private void Awake()
    {
        if (configureOnAwake)
            ConfigurePhysicsSettings();
    }

    public void ConfigurePhysicsSettings()
    {
        Time.fixedDeltaTime = fixedTimestep;

        Physics.simulationMode = SimulationMode.FixedUpdate;
        Physics.autoSyncTransforms = false; 

        Physics.defaultSolverIterations = defaultSolverIterations;
        Physics.defaultSolverVelocityIterations = defaultSolverVelocityIterations;

        Physics.defaultContactOffset = defaultContactOffset;
        Physics.bounceThreshold = bounceThreshold;
        Physics.sleepThreshold = sleepThreshold;

        Physics.defaultMaxAngularSpeed = defaultMaxAngularSpeed;

        // enhanced determinism 
        #if UNITY_2022_2_OR_NEWER
        if (useEnhancedDeterminism)
        {
            Physics.improvedPatchFriction = true;
            Physics.reuseCollisionCallbacks = reuseCollisionCallbacks;
        }
        #endif

        // set consistent gravity
        Physics.gravity = new Vector3(0, -9.81f, 0);
    }

    public static void ConfigureRigidbodyForDeterminism(Rigidbody rb)
    {
        // continuous collision detection
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.solverIterations = 8;
        rb.solverVelocityIterations = 2;

        // prevent extreme spinning
        rb.maxAngularVelocity = 50f;

        // stabilize objects
        if (rb.linearDamping < 0.05f)
            rb.linearDamping = 0.05f;

        // prevent infinite spinning
        if (rb.angularDamping < 0.05f)
            rb.angularDamping = 0.05f;
    }

    public static void SyncPhysicsTransforms()
    {
        Physics.SyncTransforms();
    }

    public static void ManualPhysicsStep()
    {
        Physics.Simulate(Time.fixedDeltaTime);
    }
}