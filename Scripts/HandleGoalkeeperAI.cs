using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class HandleGoalkeeperAI : NetworkBehaviour
{
    public enum State
    {
        idle,
        positioning,
        saving,
        passing
    }

    public enum Team
    {
        Red,
        Blue
    }

    [Header("Team Settings")]
    [SerializeField] private Team team;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float diveForce;
    [SerializeField] private float downwardGravityForce; // controls how far the goalkeeper moves to the ground after diving upwards
    [SerializeField] private float upwardsDiveForce;
    [SerializeField] private float maxMoveDistance; // controls how far the goalkeeper can move from the idle point

    [Header("Threshold Settings")]
    [SerializeField] private float diveShotSpeedThreshold; // determines how fast the ball needs to move for the keeper to make a dive save
    [SerializeField] private float savingPositionThreshold; // determines how close the ball should be for the goalkeepr to commit to a save
    [SerializeField] private float idlePositioningThreshold; // determines how far the ball needs to be for the goalkeeper to be idle
    [SerializeField] private float positioningThreshold; // determines how close the ball needs to be for the goalkeeper to be positiong for a shot
    [SerializeField] private float catchSaveBallVelThreshold; // determines the max vel a ball should be when the goalkeeper touches it for an automatic catch save
    [SerializeField] private float sweeperBallPositionThreshold;
    [SerializeField] private float dropKickPositionThreshold = 2f;

    [Header("Cooldown Settings")]
    [SerializeField] private float saveCooldown;
    [SerializeField] private float timeBeforeDownwardForce; // determines how long after a dive we apply a downward force

    [Header("Ground Check")]
    [SerializeField] private float keeperHeight;
    [SerializeField] private LayerMask groundMask;

    [Header("Prediction Settings")]
    [SerializeField] private Transform goalLine; // determines where to predict the ball's crossing point
    [SerializeField] private int predictionSteps = 100; // how many physics steps to simulate into the future
    [SerializeField] private float predictionTimeStep = 0.02f; // time interval for reach prediction step

    [Header("Ball Physics")]
    [SerializeField] private float ballDrag;
    [SerializeField] private float magnusForceMultiplier;
    [SerializeField] private float downForceMultiplier;

    [Header("Sound References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip kickSound;
    [SerializeField] private AudioClip catchSound;

    [Header("Transform References")]
    [SerializeField] private Transform ball;
    [SerializeField] private Transform idlePoint;
    [SerializeField] private Transform catchBallPos;

    [Header("Drop Kick Settings")]
    [SerializeField] private float minKickPower;
    [SerializeField] private float maxKickPower;
    [SerializeField] private float maxDistanceForMaxPower;
    [SerializeField] private float minDistanceToPassTo = 20f;

    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Rigidbody ballRb;
    [SerializeField] private BallSync ballSync;
    [SerializeField] private Animator animator;
    [SerializeField] private SphereCollider ballCollider;

    [Header("Debug Tools")]
    [SerializeField] private float ballVelocity;
    [SerializeField] private float distanceFromBall;
    [SerializeField] private Vector3 predictedLandingPoint;
    [SerializeField] private Vector3 currentBallPosition;
    public State currentState;

    private bool isSaving;
    private bool isDropKicking;
    private bool shouldBallBeInCatchPos;

    private Vector3 _calculatedTargetPosition = Vector3.zero;
    private float _directionMultiplier; // used to handle opposite team orientations

    // for animations
    private int dropKickHash;
    private int diveRightHash;
    private int diveLeftHash;
    private int sideStepRight;
    private int sideStepLeft;

    // record the direction of the dive
    private float diveDirectionZ;

    // records the ball's velocity when saving
    public float currentBallVelMag;

    private bool shouldFollowCatchBallPos;

    public bool canGoalkeeperBeDisabled = true; // for practice servers

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsHost)
            return;

        // flips the left/right logic for the blue team.
        _directionMultiplier = (team == Team.Red) ? 1f : -1f;

        isSaving = false;
        isDropKicking = false;
        _calculatedTargetPosition = transform.position;

        // animator hashes for that juicy performance boost
        dropKickHash = Animator.StringToHash("isDropKicking");
        diveRightHash = Animator.StringToHash("isDiveRight");
        diveLeftHash = Animator.StringToHash("isDiveLeft");
        sideStepRight = Animator.StringToHash("isSideStepRight");
        sideStepLeft = Animator.StringToHash("isSideStepLeft");
    }

    private void Update()
    {
        // client-side logic for attaching the ball to the keeper's hands
        if (shouldFollowCatchBallPos)
        {
            ball.position = catchBallPos.position;
            ballRb.isKinematic = true;
        }

        if (!IsHost)
            return;

        // stop moving the goalkeeper when we scored
        if (ServerManager.instance.didATeamScore.Value)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        // cruical information needed
        distanceFromBall = Vector3.Distance(ball.position, transform.position);
        ballVelocity = ballRb.linearVelocity.magnitude;
        currentBallPosition = ball.position;

        HandleMovementAnimations();

        // Host-side attachment logic
        if (shouldBallBeInCatchPos)
        {
            ball.position = catchBallPos.position;
            ball.rotation = catchBallPos.rotation;
        }

        // handle states
        switch (currentState)
        {
            case State.idle:
                HandleIdleState();
                break;
            case State.positioning:
                HandlePositioningState();
                break;
            case State.saving:
                // no need to call anything here
                break;
            case State.passing:
                HandlePassingState();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (!IsHost)
            return;

        if (!isSaving)
        {
            Vector3 directionToTarget = _calculatedTargetPosition - rb.position;
            directionToTarget.y = 0;

            if (directionToTarget.magnitude < 0.5f)
            {
                rb.linearVelocity = new(0, rb.linearVelocity.y, 0);
            }
            else
            {
                Vector3 horizontalVelocity = directionToTarget.normalized * moveSpeed;
                rb.linearVelocity = new(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
            }
        }
    }

    private void HandlePassingState()
    {
        // move to the idle position
        // the goalkeeper moving forward is handled in the animation
        _calculatedTargetPosition = new Vector3(idlePoint.position.x, rb.position.y, idlePoint.position.z);

        // check if the goalkeeper is at the idle point and not already kicking
        if ((_calculatedTargetPosition - rb.position).magnitude <= dropKickPositionThreshold && !isDropKicking)
        {
            isDropKicking = true;

            // play the animation
            animator.SetTrigger(dropKickHash);
        }
    }

    public void DropKickBall()
    {
        audioSource.PlayOneShot(kickSound);

        if (!IsHost)
            return;

        shouldBallBeInCatchPos = false;
        StopBallAttachmentOnClientsClientRpc();

        ballSync.EnableKinematics(false);
        ballSync.EnableCollider(true);

        // if there's no active player on the team
        if (team == Team.Blue && ServerManager.instance.blueTeamPlayerIds.Count < 1 ||
            team == Team.Red && ServerManager.instance.redTeamPlayerIds.Count < 1)
            DropKickBallPhysics(maxKickPower, transform.forward);

        // pass to the furthest player
        else
        {
            Transform targetTransform = GetFurthestPlayer();

            // pass to the center
            if (targetTransform == null)
            {
                DropKickBallPhysics(maxKickPower, transform.forward);
                return;
            }

            float distance = Vector3.Distance(transform.position, targetTransform.position);
            float distanceFactor = Mathf.Clamp01(distance / maxDistanceForMaxPower);
            float dynamicKickPower = Mathf.Lerp(minKickPower, maxKickPower, distanceFactor);
            Vector3 direction = (targetTransform.position - transform.position).normalized;

            DropKickBallPhysics(dynamicKickPower, direction);
        }

        // end the out of bounds play
        ballSync.EndOutOfBoundsPlayServerRpc();

        Invoke(nameof(AllowGoalkeeperToBeDisabledAgain), 2f);

        currentState = State.idle;
    }

    private Transform GetFurthestPlayer()
    {
        Transform targetTransform = null;
        ulong targetPassPlayerId = 0;

        if (team == Team.Blue)
        {
            float highestDistance = minDistanceToPassTo;

            foreach (ulong id in ServerManager.instance.blueTeamPlayerIds)
            {
                if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(id)) 
                    continue;

                Transform player = NetworkManager.Singleton.ConnectedClients[id].PlayerObject.GetComponent<PlayerInfo>().playingObj.transform;
                float distanceFromPlayer = Vector3.Distance(player.position, transform.position);

                // calculate the distance
                if (distanceFromPlayer > highestDistance)
                {
                    targetPassPlayerId = id;
                    highestDistance = distanceFromPlayer;

                    targetTransform = player;
                }
            }

            return targetTransform;
        }

        else
        {
            float highestDistance = minDistanceToPassTo;

            foreach (ulong id in ServerManager.instance.redTeamPlayerIds)
            {
                if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(id)) 
                    continue;

                Transform player = NetworkManager.Singleton.ConnectedClients[id].PlayerObject.GetComponent<PlayerInfo>().playingObj.transform;
                float distanceFromPlayer = Vector3.Distance(player.position, transform.position);

                // calculate the distance
                if (distanceFromPlayer > highestDistance)
                {
                    targetPassPlayerId = id;
                    highestDistance = distanceFromPlayer;

                    targetTransform = player;
                }
            }

            return targetTransform;
        }
    }

    private void DropKickBallPhysics(float dynamicKickPower, Vector3 direction)
    {
        // if we didnt drop the ball prior to dropkicking
        if (currentState == State.idle)
            return;

        float verticalScale = 1.2f;
        Vector3 force = dynamicKickPower * direction + verticalScale * dynamicKickPower * transform.up;

        var kickPayload = new BallSync.InputPayload
        {
            Tick = NetworkManager.Singleton.ServerTime.Tick,
            Force = force,
            AngularImpulse = Vector3.zero,
        };

        // -2 = blue team
        // -3 = red team
        ballSync.LocalKick(kickPayload, team == Team.Blue ? -2 : -3);
        currentState = State.idle;
    }

    public void ResetGoalkeeperAfterPassing()
    {
        isDropKicking = false;
    }


    [ClientRpc]
    private void BeginPassingStateClientRpc()
    {
        if (IsHost)
            return;

        shouldFollowCatchBallPos = true;
    }

    private void HandleMovementAnimations()
    {
        if (rb.linearVelocity.magnitude > 2f && !isSaving && _calculatedTargetPosition != Vector3.zero)
        {
            float moveDirectionZ = (_calculatedTargetPosition.z - transform.position.z) * _directionMultiplier;
            if (moveDirectionZ <= 0)
            {
                animator.SetBool(sideStepRight, true);
                animator.SetBool(sideStepLeft, false);
            }
            else
            {
                animator.SetBool(sideStepRight, false);
                animator.SetBool(sideStepLeft, true);
            }
        }
        else if (!isSaving || _calculatedTargetPosition == Vector3.zero)
        {
            animator.SetBool(sideStepLeft, false);
            animator.SetBool(sideStepRight, false);
        }
    }

    private void HandleIdleState()
    {
        _calculatedTargetPosition = idlePoint.position;
        if (distanceFromBall < positioningThreshold)
            currentState = State.positioning;
    }

    private void HandlePositioningState()
    {
        if (distanceFromBall > idlePositioningThreshold || IsBallBehindGoalkeeper())
        {
            currentState = State.idle;
            animator.SetBool(sideStepRight, false);
            animator.SetBool(sideStepLeft, false);
            return;
        }

        if (PredictBallTrajectory(out predictedLandingPoint))
        {
            if (distanceFromBall < savingPositionThreshold && ballVelocity > diveShotSpeedThreshold)
            {
                currentState = State.saving;
                HandleSavingState();
                return;
            }
        }

        float targetZ = ballRb.position.z;
        float clampedZ = Mathf.Clamp(targetZ, idlePoint.position.z - maxMoveDistance, idlePoint.position.z + maxMoveDistance);

        if (distanceFromBall < sweeperBallPositionThreshold)
        {
            float targetX = ballRb.position.x;

            // define clamping bounds based on team direction
            float minX, maxX;
            if (_directionMultiplier > 0) 
            {
                minX = idlePoint.position.x;
                maxX = idlePoint.position.x + maxMoveDistance;
            }
            else 
            {
                minX = idlePoint.position.x - maxMoveDistance;
                maxX = idlePoint.position.x;
            }

            float clampedX = Mathf.Clamp(targetX, minX, maxX);
            _calculatedTargetPosition = new Vector3(clampedX, rb.position.y, clampedZ);
        }
        else
            _calculatedTargetPosition = new Vector3(idlePoint.position.x, rb.position.y, clampedZ);
    }

    private bool IsBallBehindGoalkeeper()
    {
        return team == Team.Blue ? ball.position.x > transform.position.x : ball.position.x < transform.position.x;
    }

    private bool PredictBallTrajectory(out Vector3 landingPoint)
    {
        landingPoint = Vector3.zero;
        if (ballRb.linearVelocity.magnitude < 0.2f)
            return false;

        Vector3 directionToGoal = (goalLine.position - ball.position).normalized;
        if (Vector3.Dot(ballRb.linearVelocity.normalized, directionToGoal) < 0.1f)
            return false;

        Vector3 currentPosition = ball.position;
        Vector3 currentVelocity = ballRb.linearVelocity;
        Vector3 spinDirection = ballRb.angularVelocity;
        float ballMass = ballRb.mass;
        Vector3 initialRelativePosition = currentPosition - goalLine.position;

        for (int i = 0; i < predictionSteps; i++)
        {
            Vector3 magnusForce = Vector3.Cross(spinDirection, currentVelocity) * magnusForceMultiplier;
            Vector3 downForce = Vector3.down * downForceMultiplier;
            Vector3 acceleration = (magnusForce + downForce + Physics.gravity) / ballMass;
            currentVelocity += acceleration * predictionTimeStep;
            currentVelocity *= (1f - ballDrag * predictionTimeStep);
            currentPosition += currentVelocity * predictionTimeStep;
            Vector3 currentRelativePosition = currentPosition - goalLine.position;
            if (Mathf.Sign(Vector3.Dot(currentRelativePosition, goalLine.forward)) != Mathf.Sign(Vector3.Dot(initialRelativePosition, goalLine.forward)))
            {
                landingPoint = new Vector3(currentPosition.x, Mathf.Clamp(currentPosition.y, 0f, 2.5f), currentPosition.z);
                return true;
            }
        }
        return false;
    }

    private void HandleSavingState()
    {
        if (isSaving)
            return;

        isSaving = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        float clampedZ = Mathf.Clamp(predictedLandingPoint.z, idlePoint.position.z - maxMoveDistance, idlePoint.position.z + maxMoveDistance);
        Vector3 clampedDiveTarget = new(idlePoint.position.x, ballRb.position.y, clampedZ);
        currentBallVelMag = ballRb.linearVelocity.magnitude;

        float diveDirectionZ = (ballRb.position.z - transform.position.z) * _directionMultiplier;

        if (currentBallVelMag > diveShotSpeedThreshold)
        {
            float timeToIntercept = (clampedDiveTarget - ball.position).magnitude / ballRb.linearVelocity.magnitude;
            if (timeToIntercept <= 0)
            {
                StartCoroutine(DiveCooldownRoutine());
                return;
            }

            int animHashToPlay = 0;

            // play dive animation
            if (team == Team.Red)
                animHashToPlay = diveDirectionZ < 0 ? diveLeftHash : diveRightHash;
            else
                animHashToPlay = diveDirectionZ < 0 ? diveRightHash : diveLeftHash;

            animator.SetTrigger(animHashToPlay);

            Vector3 diveTargetDirection = clampedDiveTarget - transform.position;

            Vector3 horizontalForce = new Vector3(diveTargetDirection.x, 0, diveTargetDirection.z).normalized * diveForce;
            Vector3 verticalForce = new Vector3(0, diveTargetDirection.y, 0).normalized * upwardsDiveForce;

            Vector3 totalForce = horizontalForce + verticalForce;
            rb.AddForce(totalForce, ForceMode.Impulse);
        }
        else
        {
            _calculatedTargetPosition = clampedDiveTarget;
        }

        StartCoroutine(DiveCooldownRoutine());
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, keeperHeight * 0.5f + 2, groundMask);
    }

    private IEnumerator DiveCooldownRoutine()
    {
        yield return new WaitForSeconds(timeBeforeDownwardForce);
        rb.AddForce(Vector3.down * downwardGravityForce, ForceMode.Impulse);
        yield return new WaitUntil(() => IsGrounded());
        yield return new WaitForSeconds(saveCooldown);
        if (currentState == State.passing)
        {
            isSaving = false;
            yield break;
        }
        currentState = State.positioning;
        isSaving = false;
        rb.linearVelocity = Vector3.zero;
        _calculatedTargetPosition = transform.position;
    }

    [ClientRpc]
    private void HandleGoalkeeperSaveSoundClientRpc()
    {
        audioSource.PlayOneShot(catchSound);
        SoundManager.instance.PlaySaveCrowdSound();
    }

    [ClientRpc]
    private void StopBallAttachmentOnClientsClientRpc()
    {
        shouldFollowCatchBallPos = false;
    }

    public void DropBall()
    {
        shouldBallBeInCatchPos = false;
        StopBallAttachmentOnClientsClientRpc();

        ballSync.EnableKinematics(false);
        ballSync.EnableCollider(true);
        currentState = State.idle;
    }

    private void AllowGoalkeeperToBeDisabledAgain()
    {
        canGoalkeeperBeDisabled = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsHost || currentState == State.passing || ServerManager.instance.didATeamScore.Value || ServerManager.instance.isGameOver.Value || IsBallBehindGoalkeeper())
            return;

        // catching the ball
        if (collision.gameObject.layer.Equals(LayerMask.NameToLayer("Ball")))
        {
            canGoalkeeperBeDisabled = false;

            ballSync.EnableKinematics(true);
            ballSync.EnableCollider(false);

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            isSaving = false;

            HandleGoalkeeperSaveSoundClientRpc();
            StopAllCoroutines();

            currentState = State.passing;

            shouldBallBeInCatchPos = true;

            BeginPassingStateClientRpc();
        }
    }
}