using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Unity.Collections;
using System.Collections;
using UnityEngine.UI;

// note to self later: make an array of caught balls and caughtballrbs so that two goalkeepers can hold balls at the same time instead of causing a bug
public class HandleGoalkeeperAsPlayer : NetworkBehaviour
{
    [Header("Dive Settings")]
    [SerializeField] private float diveForce;
    [SerializeField] private float upwardDiveForce;
    [SerializeField] private float diveCooldown;
    [SerializeField] private float movementCooldown;
    [SerializeField] private float diveDuration = 0.7f;
    private bool isDiveCooldownOver = true;

    [Header("Check Settings")]
    [SerializeField] private float ballCatchRadius;
    [SerializeField] private float ballCatchMaxDistance;
    [SerializeField] private float catchDuration;
    private bool isCatchCooldownOver = true;

    [Header("Deflection Settings")]
    [SerializeField] private float deflectionMultiplier;
    [SerializeField] private float maxDeflectionPower = 50f;
    [SerializeField] private float minDeflectionPower = 10f;

    [Header("Dropkick Settings")]
    [SerializeField] private float dropKickForceMultiplier;
    [SerializeField] private float timeBeforeDropKicking;

    [Header("Passing Settings")]
    [SerializeField] private float passingForceMultiplier;
    [SerializeField] private float timeBeforePassing;

    [Header("Other Checks")]
    [SerializeField] private float timeBeforeAutomaticallyDroppingBall; // prevents the player from holding the ball forever
    [SerializeField] private float catchCooldownAfterDropping; // prevents the player from immediately picking up the ball again

    [Header("References")]
    [SerializeField] private AnimationStateController playerAnimation;
    [SerializeField] private HandleKicking kickingScript;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform[] gloveObjs;
    [SerializeField] private Transform catchBallPosParent;
    [SerializeField] private Slider shootingBarSlider;
    [SerializeField] private Camera cam;
    public InputActionReference diveKey;
    public InputActionReference catchKey;

    public bool isHoldingBall = false;
    public bool isGoalkeeper = false;
    public bool isDiving = false;
    private bool isInGoalkeeperBox = false;
    private bool isPerformingKick = false;
    private NetworkVariable<bool> followBallAnimationPosParent = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private GameObject caughtBall;
    private Rigidbody caughtBallRb;

    private bool isDeflectedAlready = false;

    // tracks how long the player has held the ball
    private float initialTimeWhenCaughtBall;

    public BallSync touchedBallSync;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        PlayerInfo.instance.currentPosition.OnValueChanged += OnPositionChanged;
        OnPositionChanged(default, PlayerInfo.instance.currentPosition.Value);

        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
            return;

        PlayerInfo.instance.currentPosition.OnValueChanged -= OnPositionChanged;

        base.OnNetworkDespawn();
    }

    private void Start()
    {
        if (!IsOwner)
            return;

        isDiveCooldownOver = true;
        isCatchCooldownOver = true;
    }

    private void OnPositionChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        isGoalkeeper = newValue.Equals("GK");

        // eable/disable gloves locally
        foreach (Transform glove in gloveObjs)
            glove.gameObject.SetActive(isGoalkeeper);

        if (IsServer)
            SyncGloveObjectClientRpc(isGoalkeeper);
        else
            SyncGloveObjectServerRpc(isGoalkeeper);
    }

    private void Update()
    {
        // everyone runs this code
        if (followBallAnimationPosParent.Value && caughtBall != null)
            caughtBall.transform.position = catchBallPosParent.position;

        // we also check the player's current position because there's a potential error with the event not correctly detecting the player's change into the goalkeeper position
        // due to network latency and whatnot
        if (!IsOwner || !isGoalkeeper)
            return;

        // drop the ball if we press the dive key again or if we left the box or if the ball was scored
        if (((isHoldingBall && diveKey.action.WasPressedThisFrame())
            || (isHoldingBall && !isInGoalkeeperBox) // if outside the box
            || ServerManager.instance.didATeamScore.Value  // if scored
            || (Time.time - initialTimeWhenCaughtBall > timeBeforeAutomaticallyDroppingBall) // if holding too long
            || ServerManager.instance.isStartingGame.Value // if the game is about to start, drop the ball
            || ServerManager.instance.isGameOver.Value) // if the game is over
            && !ServerManager.instance.isTutorialServer) 
            DropBall();

        if (diveKey.action.WasPressedThisFrame() && isDiveCooldownOver && isCatchCooldownOver && isInGoalkeeperBox && !isHoldingBall)
            PerformDive();

        // only allow the player to catch when the game has started
        if (catchKey.action.WasPressedThisFrame() && PlayerMovement.instance.IsOnGround && isDiveCooldownOver && isCatchCooldownOver 
            && !ServerManager.instance.didATeamScore.Value
            && !ServerManager.instance.isStartingGame.Value
            && !ServerManager.instance.isGameOver.Value)
            CatchBall();

        // handle drop kicking or passing
        if (!kickingScript.IsChargingKick && shootingBarSlider.value > 0 && isHoldingBall && isCatchCooldownOver && !isPerformingKick)
        {
            isPerformingKick = true;

            if (kickingScript.didDribble)
                StartCoroutine(HandlePassing());
            else
                StartCoroutine(HandleDropKicking());

            kickingScript.ResetShootingBar();

            playerAnimation.PlayGKHoldingBallAnimation(false);
        }

        // deflect the ball during a diving save
        if (!isDiveCooldownOver && !isDeflectedAlready)
            HandleDeflectingBall();

        // stop emote
        if (isHoldingBall)
            playerAnimation.StopCurrentEmote();
    }

    private void PerformDive()
    {
        isDiveCooldownOver = false;

        // stop emote
        playerAnimation.StopCurrentEmote();

        Vector3 playerMoveDirection = PlayerMovement.instance.GetPlayerMoveDirection();
        playerAnimation.PlayGKDiveAnimation(Vector3.Dot(playerMoveDirection.normalized, transform.right) >= 0);

        StartCoroutine(ApplyDiveForceSmoothly(playerMoveDirection));
    }

    private IEnumerator ApplyDiveForceSmoothly(Vector3 moveDirection)
    {
        float timer = 0f;

        isDiving = true;

        PlayerMovement.instance.DisableMovement(true);

        rb.AddForce(upwardDiveForce * transform.up, ForceMode.Impulse);

        while (timer < diveDuration)
        {
            float forceMultiplier = diveForce * Time.fixedDeltaTime / diveDuration;
            rb.AddForce(forceMultiplier * moveDirection, ForceMode.Force);

            timer += Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }

        isDiving = false;

        Invoke(nameof(EnableMovement), movementCooldown);
        Invoke(nameof(ResetDiveCooldown), diveCooldown);
    }

    private void ResetDiveCooldown()
    {
        isDiveCooldownOver = true;
        isDeflectedAlready = false;
    }

    private void EnableMovement()
    {
        PlayerMovement.instance.DisableMovement(false);
    }

    private void CatchBall()
    {
        if (!isInGoalkeeperBox || isHoldingBall)
            return;

        isCatchCooldownOver = false;

        playerAnimation.PlayGKHoldingBallAnimation(true);

        // stop emote
        playerAnimation.StopCurrentEmote();

        // this is animation has a higher layer priority, so only this will play
        playerAnimation.PlayGKCatchAnimation();

        RaycastHit[] hits = Physics.SphereCastAll(transform.position, ballCatchRadius, transform.forward, ballCatchMaxDistance);

        bool didFindBall = false;

        foreach (RaycastHit hit in hits)
        {
            Debug.Log(hit.collider.name);

            if (hit.collider.CompareTag("Ball"))
            {
                touchedBallSync = hit.collider.GetComponentInChildren<BallSync>();

                // still didn't find ball sync? 
                // move on
                if (touchedBallSync == null)
                    continue;

                // make sure we catch only opponent kicks and our own
                if (ServerManager.instance.isPracticeServer || 
                    ServerManager.instance.isTutorialServer ||
                    string.IsNullOrEmpty(touchedBallSync.lastKickedTeam.Value.ToString()) ||
                    !touchedBallSync.lastKickedTeam.Value.Equals(PlayerInfo.instance.currentTeam.Value) || 
                    (ulong)touchedBallSync.lastKickedClientId.Value == NetworkManager.LocalClientId)
                {
                    SoundManager.instance.PlayPlayerGKCatchSoundEffect();

                    isHoldingBall = true;
                    didFindBall = true;

                    caughtBall = hit.collider.gameObject;
                    caughtBallRb = caughtBall.GetComponent<Rigidbody>();

                    // handle timer
                    initialTimeWhenCaughtBall = Time.time;

                    // handle animation
                    HandleBallAnimationPos(true, transform.position);

                    // for tutorial stage progression
                    if (ServerManager.instance.isTutorialServer)
                        TutorialManager.instance.PassToNextDetectorInSameStage();
                }

                break;
            }
        }

        if (didFindBall)
            Invoke(nameof(ResetCatchCooldownIfCaught), catchDuration);
        else
            Invoke(nameof(ResetCatchCooldownIfDidntCatchBall), catchDuration);

        didFindBall = false;

    }

    private void ResetCatchCooldownIfCaught()
    {
        isCatchCooldownOver = true;

        if (isHoldingBall && isInGoalkeeperBox)
            playerAnimation.PlayGKHoldingBallAnimation(true);
        else
            playerAnimation.PlayGKHoldingBallAnimation(false);
    }

    private void ResetCatchCooldownIfDidntCatchBall()
    {
        isCatchCooldownOver = true;

        playerAnimation.PlayGKHoldingBallAnimation(false);
    }


    private void DropBall()
    {
        if (!isHoldingBall)
            return;

        isHoldingBall = false;
        isCatchCooldownOver = false;

        HandleBallAnimationPos(false, transform.position);

        touchedBallSync.ResetBallServerRpc(transform.position);

        playerAnimation.PlayGKHoldingBallAnimation(false);

        caughtBall = null;
        caughtBallRb = null;
        touchedBallSync = null;

        Invoke(nameof(ResetCatchCooldownIfDidntCatchBall), catchCooldownAfterDropping);
    }

    public void ForceDropBallFromServer()
    {
        if (!isHoldingBall)
            return;

        isHoldingBall = false;
        isCatchCooldownOver = false;

        HandleBallAnimationPos(false, transform.position);
        playerAnimation.PlayGKHoldingBallAnimation(false);

        caughtBall = null;
        caughtBallRb = null;
        touchedBallSync = null;
    }

    private void HandleDeflectingBall()
    {
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, ballCatchRadius, transform.forward, ballCatchMaxDistance);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.CompareTag("Ball"))
            {
                touchedBallSync = hit.collider.GetComponentInChildren<BallSync>();
                Rigidbody deflectedBallRb = hit.collider.GetComponent<Rigidbody>();

                if (deflectedBallRb == null) continue;

                NotifyServerOfDeflectionServerRpc();

                float ballVelocity = deflectedBallRb.linearVelocity.magnitude;

                SoundManager.instance.PlayPlayerGKDeflectSoundEffect();

                // calculate the deflection force dynamically
                Vector3 force = Mathf.Max(Mathf.Min(deflectionMultiplier * ballVelocity, maxDeflectionPower), minDeflectionPower) * cam.transform.forward;

                Vector3 angularImpulse = deflectedBallRb.angularVelocity;

                // network the force
                PredictAndRequestKick(force, angularImpulse);

                // for tutorial stage progression
                if (ServerManager.instance.isTutorialServer)
                    TutorialManager.instance.PassToNextDetectorInSameStage();

                PlayerInfo.instance.saves.Value++;

                isDeflectedAlready = true;
            }
        }
    }

    [ServerRpc()]
    private void NotifyServerOfDeflectionServerRpc()
    {
        ServerManager.instance.RegisterGoalkeeperSave();
    }

    private IEnumerator HandlePassing()
    {
        playerAnimation.PlayGKPassAnimation();

        float shootingBarValue = shootingBarSlider.value;

        PlayerMovement.instance.DisableMovement(true);

        yield return new WaitForSeconds(timeBeforePassing);

        PlayerMovement.instance.DisableMovement(false);

        if (!isInGoalkeeperBox)
        {
            playerAnimation.PlayGKHoldingBallAnimation(true);
            isPerformingKick = false;
            yield break;
        }

        HandleBallAnimationPos(false, transform.position);

        Vector3 force = shootingBarValue * passingForceMultiplier * transform.forward;

        PredictAndRequestKick(force, Vector3.zero);

        // for tutorial stage progression
        if (ServerManager.instance.isTutorialServer)
            TutorialManager.instance.PassToNextDetectorInSameStage();

        isHoldingBall = false;
        isPerformingKick = false;
    }

    private IEnumerator HandleDropKicking()
    {
        playerAnimation.PlayGKDropKickingAnimation();

        float shootingBarValue = shootingBarSlider.value;

        PlayerMovement.instance.DisableMovement(true);

        yield return new WaitForSeconds(timeBeforeDropKicking);

        PlayerMovement.instance.DisableMovement(false);

        if (!isInGoalkeeperBox)
        {
            playerAnimation.PlayGKHoldingBallAnimation(true); 
            isPerformingKick = false;
            yield break;
        }

        SoundManager.instance.PlayShootSoundEffect();

        HandleBallAnimationPos(false, transform.position);

        Vector3 force = shootingBarValue * dropKickForceMultiplier * cam.transform.forward;

        PredictAndRequestKick(force, Vector3.zero);

        // for tutorial stage progression
        if (ServerManager.instance.isTutorialServer)
            TutorialManager.instance.PassToNextDetectorInSameStage();

        isHoldingBall = false;
        isPerformingKick = false;
    }

    private void PredictAndRequestKick(Vector3 force, Vector3 angularImpulse)
    {
        if (touchedBallSync == null)
            return;

        var kickPayload = new BallSync.InputPayload
        {
            Tick = NetworkManager.Singleton.ServerTime.Tick,
            Force = force,
            AngularImpulse = angularImpulse,
            StopBallFirst = true
        };

        touchedBallSync.LocalKick(kickPayload, (int)NetworkManager.LocalClientId);
    }

    private void ResetShootingBar() => kickingScript.ResetShootingBar();

    private void HandleBallAnimationPos(bool condition, Vector3 gkPosition)
    {
        // locally predict
        MakeBallFollowAnimationPos(condition);

        followBallAnimationPosParent.Value = condition;

        // tell others to make the ball follow the animator pos
        if (IsHost)
            MakeBallFollowAnimationPosClientRpc(condition, gkPosition);
        else
            MakeBallFollowAnimationPosServerRpc(condition, gkPosition);
    }

    [ServerRpc]
    private void MakeBallFollowAnimationPosServerRpc(bool condition, Vector3 gkPosition) => MakeBallFollowAnimationPosClientRpc(condition, gkPosition);

    [ClientRpc]
    private void MakeBallFollowAnimationPosClientRpc(bool condition, Vector3 gkPosition)
    {
        if (IsOwner)
            return;

        // caught ball
        if (condition)
        {
            if (caughtBall == null)
            {
                caughtBall = BallManager.instance.FindNearestBall(gkPosition).gameObject;
                caughtBallRb = caughtBall.GetComponent<Rigidbody>();
            }
        }

        // drop ball
        else
        {
            caughtBall = null;
        }

        MakeBallFollowAnimationPos(condition);
    }

    private void MakeBallFollowAnimationPos(bool condition)
    {
        caughtBallRb.isKinematic = condition;
        caughtBall.transform.position = catchBallPosParent.position;
    }

    [ServerRpc]
    private void SyncGloveObjectServerRpc(bool condition) => SyncGloveObjectClientRpc(condition);

    [ClientRpc]
    private void SyncGloveObjectClientRpc(bool condition)
    {
        if (IsOwner)
            return;

        foreach (Transform glove in gloveObjs)
            glove.gameObject.SetActive(condition);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsOwner || !isGoalkeeper)
            return;

        string tag = PlayerInfo.instance.currentTeam.Value.Equals("Blue") ? "BlueGoalkeeperBoxLimiter" : "RedGoalkeeperBoxLimiter";
        if (other.CompareTag(tag))
        {
            isInGoalkeeperBox = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsOwner || !isGoalkeeper)
            return;

        string tag = PlayerInfo.instance.currentTeam.Value.Equals("Blue") ? "BlueGoalkeeperBoxLimiter" : "RedGoalkeeperBoxLimiter";
        if (other.CompareTag(tag))
        {
            isInGoalkeeperBox = false;
        }
    }
}