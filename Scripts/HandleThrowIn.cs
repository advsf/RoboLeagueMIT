using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class HandleThrowIn : NetworkBehaviour
{
    [Header("Throw In Settings")]
    [SerializeField] private float hardThrowInPower;
    [SerializeField] private float softThrowInPower;
    [SerializeField] private float timeBeforeEndingOutOfBoundsPlay = 0.2f;

    [Header("Slider Settings")]
    [SerializeField] private float sliderIncrementValue;
    [SerializeField] private Slider shootingBarSlider;

    [Header("Ball Detection Settings")]
    [SerializeField] private float ballDetectionSphereRadius;
    [SerializeField] private float ballDetectionMaxRadius;

    [Header("Animation Duration")]
    [SerializeField] private float pickUpAnimationDuration;
    [SerializeField] private float throwInAnimationDuration;

    [Header("Other References")]
    [SerializeField] private Camera cam;
    [SerializeField] private AnimationStateController playerAnimation;
    [SerializeField] private Transform animationBallPos;
    [SerializeField] private HandleKicking kickingScript;
    private GameObject positionBoundary;
    private BoxCollider antiGriefBox;

    [Header("Current Information")]
    public bool canThrowIn = false;
    public bool isPickedUp = false;

    private BallSync ballSync;
    private Rigidbody ballRb;
    private GameObject ball;

    private bool followBallPos; // follows the set ball position during an animation
    private bool isChargingUpThrow;
    private float throwSliderPower;

    private bool isOutOfBoundsDrop = false;

    private void Start()
    {
        ballSync = SceneReferenceManager.instance.ballSync;
        ballRb = SceneReferenceManager.instance.ballRb;
        positionBoundary = SceneReferenceManager.instance.positionBoundary;
        antiGriefBox = SceneReferenceManager.instance.AntiGriefBox;
        ball = SceneReferenceManager.instance.ball;

        antiGriefBox.enabled = true;
        positionBoundary.SetActive(false);
    }

    private void Update()
    {
        if (followBallPos)
        {
            ball.transform.position = animationBallPos.position;
        }

        // only possession team can throw in
        if (!IsOwner || !ServerManager.instance.possessionTeam.Value.Equals(PlayerInfo.instance.currentTeam.Value))
            return;

        if (isPickedUp && canThrowIn && !kickingScript.IsChargingKick && shootingBarSlider.value > 0)
        {
            canThrowIn = false;
            playerAnimation.PlayThrowInAnimation();
            float power = shootingBarSlider.value * (kickingScript.didDribble ? softThrowInPower : hardThrowInPower);

            // stop emote
            playerAnimation.StopCurrentEmote();

            StartCoroutine(HandleThrowingBallPhysics(power));
        }

        // handle picking up the ball
        if (!isPickedUp && !isOutOfBoundsDrop && ballSync.isOutOfBounds.Value && ballSync.isThrowIn.Value)
        {
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, ballDetectionSphereRadius, transform.forward, ballDetectionMaxRadius);

            foreach (RaycastHit hit in hits)
            {
                if (hit.rigidbody == ballRb && !ballSync.isBallPickedUp.Value)
                {
                    PickUpBall();
                    EnablePositionBoundary(true);
                    break;
                }
            }
        }
    }

    public void PickUpBall()
    {
        isPickedUp = true;
        playerAnimation.PlayPickUpAnimation();

        MakeBallFollowAnimationPos(true);
        Invoke(nameof(AllowThrowIn), pickUpAnimationDuration);
    }

    private IEnumerator HandleThrowingBallPhysics(float power)
    {
        yield return new WaitForSeconds(throwInAnimationDuration);

        MakeBallFollowAnimationPos(false);
        EnablePositionBoundary(false);

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
        Vector3 direction = ray.direction.normalized;

        PredictAndRequestKick(power * direction);

        ballSync.EndOutOfBoundsPlayServerRpc(timeBeforeEndingOutOfBoundsPlay);

        Invoke(nameof(EnablePickUp), 3f);
    }

    private void EnablePickUp() => isPickedUp = false;

    private void PredictAndRequestKick(Vector3 force)
    {
        Vector3 angularImpulse = Vector3.zero;

        var kickPayload = new BallSync.InputPayload
        {
            Tick = NetworkManager.Singleton.ServerTime.Tick,
            Force = force,
            AngularImpulse = angularImpulse
        };

        ballSync.LocalKick(kickPayload, (int)NetworkManager.LocalClientId);
    }

    private void AllowThrowIn()
    {
        canThrowIn = true;
        playerAnimation.PlayHoldingBallAnimation();
    }

    private void ResetSlider()
    {
        shootingBarSlider.gameObject.SetActive(false);
        shootingBarSlider.value = 0f;
    }

    public void MakeBallFollowAnimationPos(bool condition)
    {
        MakeBallFollowSetPositionDuringAnimation(condition);

        if (IsHost)
            MakeBallFollowSetPositionClientRpc(condition);
        else
            MakeBallFollowSetPositionServerRpc(condition);
    }

    private void MakeBallFollowSetPositionDuringAnimation(bool condition)
    {
        ballRb.isKinematic = condition;
        followBallPos = condition;
    }

    public void EnablePositionBoundary(bool condition)
    {
        antiGriefBox.enabled = false;
        positionBoundary.SetActive(condition);

        if (IsHost)
            EnablePositionBoundaryClientRpc(condition);
        else
            EnablePositionBoundaryServerRpc(condition);
    }

    [ServerRpc]
    public void EnablePositionBoundaryServerRpc(bool condition) => EnablePositionBoundaryClientRpc(condition);

    [ClientRpc]
    public void EnablePositionBoundaryClientRpc(bool condition)
    {
        if (IsOwner)
            return;

        positionBoundary.SetActive(condition);
        antiGriefBox.enabled = true;
    }

    public void DropBall()
    {
        if (!isPickedUp)
            return;

        isPickedUp = false;
        canThrowIn = false;
        isOutOfBoundsDrop = true;

        EnablePositionBoundary(false);

        playerAnimation.DropBallAnimation();

        Vector3 angularImpulse = Vector3.zero;
        var kickPayload = new BallSync.InputPayload
        {
            Tick = NetworkManager.Singleton.LocalTime.Tick,
            Force = Vector3.zero,
            AngularImpulse = angularImpulse
        };

        ballSync.LocalKick(kickPayload, (int)NetworkManager.LocalClientId);

        MakeBallFollowAnimationPos(false);

        // reset drop-block after short delay
        Invoke(nameof(ResetOutOfBoundsDrop), 0.3f);
    }

    private void ResetOutOfBoundsDrop()
    {
        isOutOfBoundsDrop = false;
    }

    [ServerRpc]
    private void MakeBallFollowSetPositionServerRpc(bool condition) => MakeBallFollowSetPositionClientRpc(condition);

    [ClientRpc]
    private void MakeBallFollowSetPositionClientRpc(bool condition) => MakeBallFollowSetPositionDuringAnimation(condition);

    public void EndOutOfBoundsPlay() => ballSync.EndOutOfBoundsPlayServerRpc();
}
