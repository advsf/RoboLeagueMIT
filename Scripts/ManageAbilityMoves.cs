using UnityEngine;
using Unity.Netcode;
using System.Collections;
using TMPro;

public class ManageAbilityMoves : NetworkBehaviour
{
    [Header("Rouletter References")]
    [SerializeField] private Transform rouletteBallPos;
    [SerializeField] private float firstSoundTimeStamp;
    [SerializeField] private float secondSoundTimeStamp;

    [Header("Trap References")]
    [SerializeField] private Transform highTrapPosition;
    [SerializeField] private Transform lowTrapPosition;

    [Header("Kick References")]
    [SerializeField] private Transform sphereCastOrigin;
    [SerializeField] private float stumbleDuration;
    [SerializeField] private float gettingUpDuration = 1.25f;

    [Header("Deflect References")]
    [SerializeField] private float timeBeforeKick = 0.35f;
    [SerializeField] private float distanceToballThreshold;
    [SerializeField] private float minYHeightOfBallToDeflect;

    [Header("Ability Message UI References")]
    [SerializeField] private GameObject abilityMessageUIObj;
    [SerializeField] private TextMeshProUGUI abilityMessageText;

    [Header("Other References")]
    [SerializeField] private AnimationStateController animationController;
    [SerializeField] private Rigidbody playerRb;

    private Rigidbody ballRb;

    private bool shouldFollowRouletteBallPos;

    public override void OnNetworkSpawn()
    {
        abilityMessageUIObj.SetActive(false);
        abilityMessageText.text = "";

        base.OnNetworkSpawn();
    }

    private void LateUpdate()
    {
        // for the roulette
        if (shouldFollowRouletteBallPos)
        {
            ballRb.isKinematic = true;
            ballRb.transform.position = rouletteBallPos.position;
        }
    }

    #region Abilities

    public void Deflect(float deflectSphereRadius, float deflectMaxDistance, float playerMoveSpeed, float ballKickForce, float lowerCooldown, Deflect deflect)
    {
        var ballSynchronizer = BallManager.instance.FindNearestBall(transform.position);
        ballRb = ballSynchronizer.GetRigidbody();

        // stop emote
        animationController.StopCurrentEmote();

        // to make sure we can't deflect our own shot or goalkicks or low ground balls
        if (((ballSynchronizer.lastKickedClientId.Value != (int)OwnerClientId && ballSynchronizer.lastKickedClientId.Value >= 0) || ServerManager.instance.isTutorialServer) && ballRb.transform.position.y >= minYHeightOfBallToDeflect)
        {
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, deflectSphereRadius, transform.forward, deflectMaxDistance);

            foreach (RaycastHit hit in hits)
            {
                if (hit.rigidbody == ballRb)
                {
                    // for tutorial stage progression
                    if (ServerManager.instance.isTutorialServer)
                        TutorialManager.instance.PassToNextDetectorInSameStage();

                    StartCoroutine(HandleDeflectAbility(ballSynchronizer, deflect, transform, playerMoveSpeed, ballKickForce));
                    return;
                }
            }
        }

        // make a error message
        if (ballSynchronizer.lastKickedClientId.Value == (int)OwnerClientId)
            HandleAbilityMessageUI("Can't deflect your own shot!");

        else if (ballSynchronizer.lastKickedClientId.Value < 0 && !ServerManager.instance.isTutorialServer)
            HandleAbilityMessageUI("Can't deflect goal kicks!");

        else if (ballRb.transform.position.y < minYHeightOfBallToDeflect)
            HandleAbilityMessageUI("Can't deflect low balls!");
        else
            HandleAbilityMessageUI("Too far!");

        // lower cooldown if we couldn't deflect
        StartCoroutine(StartCustomCooldown(0, lowerCooldown, deflect));
    }

    public void Kick(float sphereRadius, float sphereMaxDistance, float kickForce, Vector3 direction, float delay, float timeToReEnableMovement, Kick kick)
    {
        // must be on the ground to perform the move
        if (!PlayerMovement.instance.IsOnGround)
        {
            // make a error message
            HandleAbilityMessageUI("Must be on the ground!");
            StartCoroutine(StartCustomCooldown(0, 2f, kick));
            return;
        }
        
        if (!ServerManager.instance.didStartGame.Value && !ServerManager.instance.isTutorialServer && !ServerManager.instance.isPracticeServer)
        {
            // make a error message
            HandleAbilityMessageUI("Cannot use the ability before the game starts!");
            StartCoroutine(StartCustomCooldown(0, 2f, kick));
            return;
        }

        // play the kick animation
        animationController.PlayKickAnimation();

        // stop emote
        animationController.StopCurrentEmote();

        SoundManager.instance.PlaySwooshSoundEffect();

        // stop moving the player
        PlayerMovement.instance.DisableMovement(true);
        playerRb.linearVelocity = Vector3.zero;

        RaycastHit[] hits = Physics.SphereCastAll(transform.position, sphereRadius, transform.forward, sphereMaxDistance);

        foreach (RaycastHit hit in hits)
        {
            Transform hitObj = hit.transform.root;
            
            if (!ServerManager.instance.isTutorialServer)
            {
                // if we hit something that is a player and not on the same team
                if (hitObj.GetComponent<NetworkObject>() && hitObj.GetComponent<NetworkObject>().OwnerClientId != OwnerClientId && !hitObj.GetComponent<PlayerInfo>().currentTeam.Value.Equals(PlayerInfo.instance.currentTeam.Value))
                {
                    ulong hitClientId = hit.transform.root.GetComponent<NetworkObject>().OwnerClientId;
                    PlayerInfo hitInfo = hit.transform.root.GetComponent<PlayerInfo>();

                    // we cannot kick goalkeeprs
                    if (hitInfo.currentPosition.Equals("GK"))
                    {
                        HandleAbilityMessageUI("Cannot kick goalkeepers!");
                        StartCoroutine(StartCustomCooldown(0, 2f, kick));
                        return;
                    }

                    // play a hit sound effect
                    SoundManager.instance.PlayPunchSoundEffect();

                    // send message to the hit client
                    if (IsHost)
                        StumblePlayerClientRpc(kickForce, direction, new ClientRpcParams
                        {
                            Send = new ClientRpcSendParams
                            {
                                TargetClientIds = new ulong[] { hitClientId }
                            }
                        });
                    else
                        StumblePlayerServerRpc(kickForce, direction, hitClientId);

                    break;
                }
            }

            // if tutorial stage
            // then we detect npc and make the npc play the animation i guess
            else if (hitObj.CompareTag("TutorialBot"))
            {
                // play a hit sound effect
                SoundManager.instance.PlayPunchSoundEffect();

                // for tutorial stage progression
                if (ServerManager.instance.isTutorialServer)
                    TutorialManager.instance.PassToNextDetectorInSameStage();

                StartCoroutine(HandleStumbleLogicForTutorialBot(hitObj.gameObject, kickForce, direction));
            }
        }

        Invoke(nameof(EnablePlayerMovement), timeToReEnableMovement);

        StartCoroutine(StartCooldown(delay, kick));
    }

    public void Roulette(float moveToBallForce, float exitBallForce, float rouletteAnimationDuration, float speedBoostAmount, float speedBoostDuration, float autoCancelCooldown, float rouletteBallSphereRadius, float rouletteBallMaxDistance, float delay, Roulette roulette)
    {
        if (!PlayerMovement.instance.IsOnGround)
        {
            HandleAbilityMessageUI("Must be on the ground!");
            StartCoroutine(StartCustomCooldown(0, 2f, roulette));
            return;
        }

        var ballSynchronizer = BallManager.instance.FindNearestBall(transform.position);
        ballRb = ballSynchronizer.GetRigidbody();

        // stop emote
        animationController.StopCurrentEmote();

        if (ballSynchronizer.lastKickedClientId.Value == (int) OwnerClientId)
        {
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, rouletteBallSphereRadius, transform.forward, rouletteBallMaxDistance);

            foreach (RaycastHit hit in hits)
            {
                if (hit.rigidbody == ballRb)
                {
                    SoundManager.instance.PlayRouletteSoundEffect();

                    // move the player to the ball
                    Vector3 directionToBall = (ballRb.position - playerRb.position).normalized;
                    directionToBall.y = 0f; 
                    playerRb.AddForce(directionToBall * moveToBallForce, ForceMode.Impulse);

                    ballSynchronizer.StopBall();

                    // prevent sliding
                    playerRb.linearVelocity = Vector3.zero;

                    // stop the ball
                    ballSynchronizer.StopBall();

                    animationController.PlayRouletteAnimation();

                    StartCoroutine(FollowRouletteBallPos(ballSynchronizer, rouletteAnimationDuration, speedBoostAmount, speedBoostDuration, exitBallForce));

                    // handle audio
                    Invoke(nameof(PlayDribbleSoundEffect), firstSoundTimeStamp);
                    Invoke(nameof(PlayDribbleSoundEffect), secondSoundTimeStamp);

                    // for tutorial stage progression
                    if (ServerManager.instance.isTutorialServer)
                        TutorialManager.instance.PassToNextDetectorInSameStage();

                    // only start the cooldown when we performed the move
                    if (ServerManager.instance.didStartGame.Value)
                        StartCoroutine(StartCooldown(delay, roulette));

                    // no cooldown for practice sessions for before the game starts
                    else
                        StartCoroutine(StartCustomCooldown(delay, 1, roulette));

                    return;
                }
            }
        }

        // handle the error message
        if (ballSynchronizer.lastKickedClientId.Value != (int)OwnerClientId)
            HandleAbilityMessageUI("You must be the last kicker!");

        else
            HandleAbilityMessageUI("Not in range!");

        // play the animation again to show feedback
        animationController.PlayRouletteAnimation();

        // lower cd if we couldn't actually perform the roulette 
        StartCoroutine(StartCustomCooldown(0, autoCancelCooldown, roulette));
    }

    public void Trap(float trapBallSphereRadius, float trapBallSphereMaxDistance, float highTrapBallYThreshold, float movementDelay, float delay, Trap trap)
    {
        var ballSynchronizer = BallManager.instance.FindNearestBall(transform.position);
        ballRb = ballSynchronizer.GetRigidbody();

        // stop emote
        animationController.StopCurrentEmote();

        RaycastHit[] hits = Physics.SphereCastAll(transform.position, trapBallSphereRadius, transform.forward, trapBallSphereMaxDistance);

        Vector3 position = transform.position;

        // stop moving the player IF we are on the ground
        if (PlayerMovement.instance.IsOnGround)
        {
            PlayerMovement.instance.DisableMovement(true);
            playerRb.linearVelocity = Vector3.zero;
        }

        // if the ball is in the air OR we are not grounded
        if (ballRb.position.y > highTrapBallYThreshold || !PlayerMovement.instance.IsOnGround)
        {
            animationController.PlayHighTrapAnimation();
            position = highTrapPosition.position;
        }

        else
        {
            animationController.PlayLowTrapAnimation();
            position = lowTrapPosition.position;
        }


        foreach (RaycastHit hit in hits)
        {
            if (hit.rigidbody == ballRb)
            {
                SoundManager.instance.PlayTrapSoundEffect();
               
                // host syncs with the other clients
                ballSynchronizer.ResetBallServerRpc(position);

                // for tutorial stage progression
                if (ServerManager.instance.isTutorialServer)
                    TutorialManager.instance.PassToNextDetectorInSameStage();

                break;
            }
        }

        Invoke(nameof(EnablePlayerMovement), movementDelay);

        StartCoroutine(StartCooldown(delay, trap));
    }

    public void Speedster(float speedIncrease, float duration, Speedster speedster)
    {
        if (ServerManager.instance.didStartGame.Value || ServerManager.instance.isPracticeServer || ServerManager.instance.isTutorialServer)
        {
            HandleAbilityMessageUI("Increased speed!");
            PlayerMovement.instance.IncreaseSpeedForDuration(speedIncrease, duration, speedster);
        }

        else
        {
            HandleAbilityMessageUI("Cannot use this ability before the game starts!");
            StartCoroutine(TriggerDelayedCooldown(2f, 1f, speedster));
        }
    }

    private IEnumerator TriggerDelayedCooldown(float delay, float cooldown, Speedster speedster)
    {
        yield return new WaitForSeconds(delay);

        HandleAbilities.instance.TriggerCooldown(speedster, cooldown);
    }

    #endregion

    private void EnablePlayerMovement()
    {
        PlayerMovement.instance.DisableMovement(false);
    }

    public IEnumerator StartCooldown(float delay, Abilities abilityUsed)
    {
        yield return new WaitForSeconds(delay);

        HandleAbilities.instance.TriggerCooldown(abilityUsed);
    }

    public IEnumerator StartCustomCooldown(float delay, float cooldown, Abilities abilityUsed)
    {
        yield return new WaitForSeconds(delay);

        HandleAbilities.instance.TriggerCooldown(abilityUsed, cooldown);
    }

    public IEnumerator FollowRouletteBallPos(BallSync ballSynchronizer, float animationDuration, float speedBoostAmount, float speedBoostDuration, float ballExitForce)
    {
        float startTime = Time.time;
        ballRb = BallManager.instance.FindNearestBall(transform.position).GetComponent<Rigidbody>();
        ballRb.isKinematic = true;

        shouldFollowRouletteBallPos = true;

        // tell other clients to move the ball
        if (IsHost)
            MoveBallInRouletteClientRpc();
        else
            MoveBallInRouletteServerRpc();

        float currentTime = Time.time - startTime;

        // the way we handle the ball moving is handled in update
        while (currentTime <= animationDuration)
        {
            currentTime = Time.time - startTime;

            yield return null;
        }

        ballRb.isKinematic = false;
        shouldFollowRouletteBallPos = false;

        // sync end of animation
        if (IsHost)
            AllowBallToMoveAfterRoulleteClientRpc(transform.position);
        else
            AllowBallToMoveAfterRoulleteServerRpc(transform.position);

        Vector3 force = ballExitForce * transform.forward;

        var kickPayload = new BallSync.InputPayload
        {
            Tick = NetworkManager.Singleton.ServerTime.Tick,
            Force = force,
            AngularImpulse = Vector3.zero,
        };

        ballSynchronizer.LocalKick(kickPayload, (int)NetworkManager.LocalClientId);

        // give a little speed boost
        PlayerMovement.instance.IncreaseSpeedForDuration(speedBoostAmount, speedBoostDuration);
    }

    private IEnumerator HandleDeflectAbility(BallSync ballSynchronizer, Deflect deflect, Transform player, float playerMoveSpeed, float ballKickForce)
    {
        // stop the ball
        ballSynchronizer.StopBall();
        ballSynchronizer.EnableKinematics(true);

        playerRb.useGravity = false;

        animationController.PlayDeflectAnimation();

        float distanceToball = Vector3.Distance(playerRb.position, ballRb.position);

        playerRb.useGravity = false;

        // move the player to the ball
        while (distanceToball >= distanceToballThreshold)
        {
            Vector3 directionToBall = (ballRb.position - playerRb.position).normalized;
            playerRb.AddForce(directionToBall * playerMoveSpeed, ForceMode.Force);

            playerRb.linearVelocity = Vector3.zero;

            distanceToball = Vector3.Distance(playerRb.position, ballRb.position);

            yield return new WaitForFixedUpdate();
        }

        yield return new WaitForSeconds(timeBeforeKick);

        playerRb.useGravity = true;
        ballSynchronizer.EnableKinematics(false);

        var kickPayload = new BallSync.InputPayload
        {
            Tick = NetworkManager.Singleton.LocalTime.Tick,
            Force = player.forward * ballKickForce,
            AngularImpulse = Vector3.zero,
        };


        ballSynchronizer.LocalKick(kickPayload, (int)NetworkManager.LocalClientId);

        SoundManager.instance.PlayShootSoundEffect();

        StartCoroutine(StartCooldown(0, deflect));
    }

    private void HandleAbilityMessageUI(string message)
    {
        abilityMessageUIObj.SetActive(true);
        abilityMessageText.text = message;

        SoundManager.instance.PlayDingSound();

        Invoke(nameof(RemoveAbilityMessageUI), 2f);
    }

    private void RemoveAbilityMessageUI() => abilityMessageUIObj.SetActive(false);

    [ServerRpc]
    private void MoveBallInRouletteServerRpc()
    {
        MoveBallInRouletteClientRpc();
    }

    [ClientRpc]
    private void MoveBallInRouletteClientRpc()
    {
        shouldFollowRouletteBallPos = true;
    }

    [ServerRpc]
    private void AllowBallToMoveAfterRoulleteServerRpc(Vector3 playerPosition) => AllowBallToMoveAfterRoulleteClientRpc(playerPosition);

    [ClientRpc]
    private void AllowBallToMoveAfterRoulleteClientRpc(Vector3 playerPosition)
    {
        if (IsOwner)
            return;

        ballRb = BallManager.instance.FindNearestBall(playerPosition).GetComponent<Rigidbody>();

        shouldFollowRouletteBallPos = false;
        ballRb.isKinematic = false;
    }

    [ServerRpc]
    private void StumblePlayerServerRpc(float kickForce, Vector3 direction, ulong hitClientId)
    {
        StumblePlayerClientRpc(kickForce, direction, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { hitClientId }
            }
        });
    }

    [ClientRpc]
    private void StumblePlayerClientRpc(float kickForce, Vector3 direction, ClientRpcParams clientRpcParams = default)
    {
        StartCoroutine(HandleStumbleLogic(kickForce, direction));
    }

    private IEnumerator HandleStumbleLogic(float kickForce, Vector3 direction)
    {
        GameObject playerObj = NetworkManager.LocalClient.PlayerObject.gameObject;
        AnimationStateController animationController = playerObj.GetComponentInChildren<AnimationStateController>();
        Rigidbody playerRb = playerObj.GetComponentInChildren<Rigidbody>();

        // disable movement
        PlayerMovement.instance.DisableMovement(true);

        // stop emoting
        PlayerInfo.instance.gameObject.GetComponentInChildren<AnimationStateController>().StopCurrentEmote();

        playerRb.AddForce(kickForce * direction.normalized, ForceMode.Impulse);
        playerRb.linearVelocity = Vector3.zero;
        animationController.PlayFallingAnimation();

        yield return new WaitForSeconds(stumbleDuration);

        animationController.StopFallingAnimation();

        yield return new WaitForSeconds(gettingUpDuration);

        PlayerMovement.instance.DisableMovement(false);
    }

   
    private IEnumerator HandleStumbleLogicForTutorialBot(GameObject bot, float kickForce, Vector3 direction)
    {
        Rigidbody botRb = bot.GetComponent<Rigidbody>();
        AnimationStateController botAnimation = bot.GetComponentInChildren<AnimationStateController>();

        botRb.AddForce(kickForce * direction.normalized, ForceMode.Impulse);
        botRb.linearVelocity = Vector3.zero;

        botAnimation.PlayFallingAnimation();

        yield return new WaitForSeconds(stumbleDuration);

        botAnimation.StopFallingAnimation();

        yield return new WaitForSeconds(gettingUpDuration);
    }

    // used in the roulette ability
    private void PlayDribbleSoundEffect() => SoundManager.instance.PlayDribbleSoundEffect();
}
