using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class AnimationStateController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Settings")]
    [SerializeField] private bool isNPC = false;
    [SerializeField] private float acceleration = 2.0f;
    [SerializeField] private float deceleration = 2.0f;
    [SerializeField] private float maxWalkVelocity = 0.5f;
    [SerializeField] private float maxRunVelocity = 2.0f;

    private float velocityX = 0.0f;
    private float velocityZ = 0.0f;
    private float currentMaxVelocity;

    // walking/running animation
    private int velocityXHash;
    private int velocityZHash;

    // shooting
    private int isShotAnimation;
    private int isPowerKickedHash;

    // movement
    private int isJumpedHash;
    private int isDashedHash;
    private int isSlidingHash;

    // abilities
    private int highTrapAnimationHash;
    private int lowTrapAnimationHash;
    private int isRouletteAnimationHash;
    private int isKickingAnimationHash;
    private int isFallingAnimationHash;
    private int isDeflectingAnimationHash;

    // throw ins
    private int pickUpAnimationHash;
    private int throwAnimationHash;
    private int holdingBallHash;
    private int dropBallHash;

    // dribbling
    private int dribbleForwardLeftHash;
    private int dribbleForwardRightHash;
    private int dribbleForwardSideLeftHash;
    private int dribbleForwardSideRightHash;
    private int dribbleChopLeftHash;
    private int dribbleChopRightHash;

    // player goalkeeping
    private int playerGKRightDiveHash;
    private int playerGKLeftDiveHash;
    private int playerGKPassHash;
    private int playerGKDropKickHash;
    private int playerGKCatchHash;
    private int playerGKDropBallHash;
    private int playerGKHoldingBallHash;

    // emotes
    private int emote1Hash;
    private int emote2Hash;
    private int emote3Hash;
    private int emote4Hash;
    private Coroutine currentEmoteCoroutine;
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        velocityXHash = Animator.StringToHash("Velocity X");
        velocityZHash = Animator.StringToHash("Velocity Z");

        isShotAnimation = Animator.StringToHash("isShooting");
        isJumpedHash = Animator.StringToHash("isJumping");
        isPowerKickedHash = Animator.StringToHash("isPowerKicked");

        isDashedHash = Animator.StringToHash("isDashed");
        isSlidingHash = Animator.StringToHash("isSliding");

        highTrapAnimationHash = Animator.StringToHash("isHighTrapping");
        lowTrapAnimationHash = Animator.StringToHash("isLowTrapping");

        isRouletteAnimationHash = Animator.StringToHash("isRouletting");

        isKickingAnimationHash = Animator.StringToHash("isKicking");
        isFallingAnimationHash = Animator.StringToHash("isFalling");

        isDeflectingAnimationHash = Animator.StringToHash("isDeflecting");

        pickUpAnimationHash = Animator.StringToHash("isPickingUpBall");
        throwAnimationHash = Animator.StringToHash("isThrowingBall");
        holdingBallHash = Animator.StringToHash("isHoldingBall");

        dropBallHash = Animator.StringToHash("dropBall");

        dribbleForwardLeftHash = Animator.StringToHash("isDribblingForwardLeft");
        dribbleForwardRightHash = Animator.StringToHash("isDribblingForwardRight");
        dribbleForwardSideLeftHash = Animator.StringToHash("isDribblingForwardSideLeft");
        dribbleForwardSideRightHash = Animator.StringToHash("isDribblingForwardSideRight");
        dribbleChopLeftHash = Animator.StringToHash("isDribblingChopLeft");
        dribbleChopRightHash = Animator.StringToHash("isDribblingChopRight");

        playerGKRightDiveHash = Animator.StringToHash("isGKRightDiving");
        playerGKLeftDiveHash = Animator.StringToHash("isGKLeftDiving");
        playerGKPassHash = Animator.StringToHash("isGKPassing");
        playerGKDropKickHash = Animator.StringToHash("isGKDropKicking");
        playerGKCatchHash = Animator.StringToHash("isGKCatching");
        playerGKDropBallHash = Animator.StringToHash("isGKDroppingBall");
        playerGKHoldingBallHash = Animator.StringToHash("isGKHoldingBall");

        emote1Hash = Animator.StringToHash("isEmoting1");
        emote2Hash = Animator.StringToHash("isEmoting2");
        emote3Hash = Animator.StringToHash("isEmoting3");
        emote4Hash = Animator.StringToHash("isEmoting4");
    }

    private void Update()
    {
        if (!IsOwner || isNPC)
            return;

        if (HandleCursorSettings.instance.IsUIOn())
        {
            velocityX = Mathf.Lerp(velocityX, 0f, Time.deltaTime * deceleration);
            velocityZ = Mathf.Lerp(velocityZ, 0f, Time.deltaTime * deceleration);

            animator.SetFloat(velocityXHash, velocityX);
            animator.SetFloat(velocityZHash, velocityZ);

            return;
        }

        // set current maxVelocity
        currentMaxVelocity = PlayerMovement.instance.IsSprinting ? maxRunVelocity : maxWalkVelocity;

        // handle animation changes (with blend tree)
        ChangeVelocity();
        LockOrResetVelocity();

        // send the data
        animator.SetFloat(velocityXHash, velocityX);
        animator.SetFloat(velocityZHash, velocityZ);

        // stop the emote if we are moving or on the air
        if (PlayerMovement.instance.isWalking || PlayerMovement.instance.isSprinting || PlayerMovement.instance.IsStrafingLeft || PlayerMovement.instance.IsStrafingRight || !PlayerMovement.instance.IsOnGround)
            StopCurrentEmote();
    }

    #region public animations

    public void PlayShootAnimation() => animator.SetTrigger(isShotAnimation);

    public void PlayJumpAnimation() => animator.SetTrigger(isJumpedHash);

    public void PlayDashAnimation() => animator.SetTrigger(isDashedHash);

    public void PlaySlideAnimation() => animator.SetBool(isSlidingHash, true);

    public void StopSlideAnimation() => animator.SetBool(isSlidingHash, false);

    public void PlayHighTrapAnimation() => animator.SetTrigger(highTrapAnimationHash);

    public void PlayLowTrapAnimation() => animator.SetTrigger(lowTrapAnimationHash);

    public void PlayPowerKickAnimation() => animator.SetTrigger(isPowerKickedHash);

    public void PlayRouletteAnimation() => animator.SetTrigger(isRouletteAnimationHash);

    public void PlayKickAnimation() => animator.SetTrigger(isKickingAnimationHash);

    public void PlayFallingAnimation() => animator.SetBool(isFallingAnimationHash, true);

    public void StopFallingAnimation() => animator.SetBool(isFallingAnimationHash, false);

    public void PlayDeflectAnimation() => animator.SetTrigger(isDeflectingAnimationHash);

    public void PlayPickUpAnimation() => animator.SetTrigger(pickUpAnimationHash);

    public void PlayThrowInAnimation() => animator.SetTrigger(throwAnimationHash);

    public void PlayHoldingBallAnimation() => animator.SetTrigger(holdingBallHash);

    public void DropBallAnimation() => animator.SetTrigger(dropBallHash);

    public void PlayDribbleAnimation()
    {
        PlayerMovement playerMovement = PlayerMovement.instance;

        // only forward dribble
        if (playerMovement.IsWalkingForward && !(playerMovement.IsStrafingLeft || playerMovement.IsStrafingRight))
        {
            if (velocityX > 0.01f)
                animator.SetTrigger(dribbleForwardRightHash);
            else
                animator.SetTrigger(dribbleForwardLeftHash);
        }

        // forward side dribble
        else if (playerMovement.IsWalkingForward && (playerMovement.IsStrafingLeft || playerMovement.IsStrafingRight))
        {
            if (velocityX > 0.01f)
                animator.SetTrigger(dribbleForwardSideRightHash);
            else
                animator.SetTrigger(dribbleForwardSideLeftHash);
        }

        // chop dribble
        else if (!playerMovement.IsWalkingForward && (playerMovement.IsStrafingLeft || playerMovement.IsStrafingRight))
        {
            if (playerMovement.IsStrafingRight)
                animator.SetTrigger(dribbleChopRightHash);

            else if (playerMovement.IsStrafingLeft)
                animator.SetTrigger(dribbleChopLeftHash);
        }

        // default dribbling
        else
            animator.SetTrigger(dribbleForwardRightHash);
    }

    public void PlayGKDiveAnimation(bool isMovingRight) => animator.SetTrigger(isMovingRight ? playerGKRightDiveHash : playerGKLeftDiveHash);

    public void PlayGKPassAnimation() => animator.SetTrigger(playerGKPassHash);

    public void PlayGKDropKickingAnimation() => animator.SetTrigger(playerGKDropKickHash);

    public void PlayGKCatchAnimation() => animator.SetTrigger(playerGKCatchHash);

    public void PlayGKDropBallAnimation() => animator.SetTrigger(playerGKDropBallHash);

    public void PlayGKHoldingBallAnimation(bool condition) => animator.SetBool(playerGKHoldingBallHash, condition);

    #endregion

    # region emote animations

    public void PlayEmote(int emoteNumber, float duration)
    {
        int hash = GetEmoteHash(emoteNumber);

        // already playing so do nothing
        if (animator.GetBool(hash))
            return;

        // if we are currently playing an emote
        StopCurrentEmote();

        if (emoteNumber == 2)
            SoundManager.instance.PlayEmote2Song(true);

        animator.SetBool(hash, true);

        // start coroutine to automatically stop this emote
        currentEmoteCoroutine = StartCoroutine(StopEmoteAfterDuration(hash, duration));
    }

    private IEnumerator StopEmoteAfterDuration(int emoteHash, float duration)
    {
        yield return new WaitForSeconds(duration);
        animator.SetBool(emoteHash, false);
    }

    public void StopCurrentEmote()
    {
        // stop any running coroutine
        if (currentEmoteCoroutine != null)
        {
            StopCoroutine(currentEmoteCoroutine);
            currentEmoteCoroutine = null;
        }

        // stop animation
        animator.SetBool(emote1Hash, false);
        animator.SetBool(emote2Hash, false);
        animator.SetBool(emote3Hash, false);
        animator.SetBool(emote4Hash, false);

        // also stop all the sound
        SoundManager.instance.PlayEmote2Song(false);
    }

    private int GetEmoteHash(int emoteNumber)
    {
        return emoteNumber switch
        {
            1 => emote1Hash,
            2 => emote2Hash,
            3 => emote3Hash,
            4 => emote4Hash,
            _ => throw new System.NotImplementedException()
        };
    }

    #endregion

    private void ChangeVelocity()
    {
        // increase velocity in z direction (walking animation)
        if (PlayerMovement.instance.IsWalking && velocityZ < currentMaxVelocity)
            velocityZ += Time.deltaTime * acceleration;

        // increase velocitiy in left direction (left strafe walking animation)
        if (PlayerMovement.instance.IsStrafingLeft && velocityX > -currentMaxVelocity)
            velocityX -= Time.deltaTime * acceleration;

        // increase velocity in right direction (right strafe walking animation)
        if (PlayerMovement.instance.IsStrafingRight && velocityX < currentMaxVelocity)
            velocityX += Time.deltaTime * acceleration;

        // decrease velocityZ
        if (!PlayerMovement.instance.IsWalking && velocityZ > 0.0f)
            velocityZ -= Time.deltaTime * deceleration;

        // increase velocityX if not left strafing
        if (!PlayerMovement.instance.IsStrafingLeft && velocityX < 0.0f)
            velocityX += Time.deltaTime * deceleration;

        // decrease velocityX if not right strafing
        if (!PlayerMovement.instance.IsStrafingRight && velocityX > 0.0f)
            velocityX -= Time.deltaTime * deceleration;
    }

    private void LockOrResetVelocity()
    {
        // reset velocity
        if (!PlayerMovement.instance.IsWalking && velocityZ < 0.0f)
            velocityZ = 0.0f;

        // reset velocityX
        if (!PlayerMovement.instance.IsStrafingRight && !PlayerMovement.instance.IsStrafingLeft && velocityX != 0.0f && (velocityX > -0.05f && velocityX < 0.05f))
            velocityX = 0.0f;

        // lock velocityZ
        if (PlayerMovement.instance.IsWalking && PlayerMovement.instance.IsSprinting && velocityZ > currentMaxVelocity)
            velocityZ = currentMaxVelocity;

        // deceleraate to the max walk velocity
        else if (PlayerMovement.instance.IsWalking && velocityZ > currentMaxVelocity)
        {
            velocityZ -= Time.deltaTime * deceleration;

            // round to currentMaxVelocity if within offset
            if (velocityZ > currentMaxVelocity && velocityZ < (currentMaxVelocity + 0.05f))
                velocityZ = currentMaxVelocity;
        }

        // round to currentMaxVelocity if within offset
        else if (PlayerMovement.instance.IsWalking && velocityZ < currentMaxVelocity && velocityZ > (currentMaxVelocity - 0.05f))
            velocityZ = currentMaxVelocity;

        // lock left
        if (PlayerMovement.instance.IsStrafingLeft && PlayerMovement.instance.IsSprinting && velocityX < -currentMaxVelocity)
            velocityX = -currentMaxVelocity;

        // deceleraate to the max walk velocity
        else if (PlayerMovement.instance.IsStrafingLeft && velocityX < -currentMaxVelocity)
        {
            velocityX += Time.deltaTime * deceleration;

            // round to currentMaxVelocity if within offset
            if (velocityX < -currentMaxVelocity && velocityX > (-currentMaxVelocity + 0.05f))
                velocityX = -currentMaxVelocity;
        }

        // round to currentMaxVelocity if within offset
        else if (PlayerMovement.instance.IsStrafingLeft && velocityX > -currentMaxVelocity && velocityX < (-currentMaxVelocity - 0.05f))
            velocityX = -currentMaxVelocity;

        // lock right
        if (PlayerMovement.instance.IsStrafingRight && PlayerMovement.instance.IsSprinting && velocityX > currentMaxVelocity)
            velocityX = currentMaxVelocity;

        // deceleraate to the max walk velocity
        else if (PlayerMovement.instance.IsStrafingRight && velocityX > currentMaxVelocity)
        {
            velocityX -= Time.deltaTime * deceleration;

            // round to currentMaxVelocity if within offset
            if (velocityX > currentMaxVelocity && velocityX < (currentMaxVelocity + 0.05f))
                velocityX = currentMaxVelocity;
        }

        // round to currentMaxVelocity if within offset
        else if (PlayerMovement.instance.IsStrafingRight && velocityX < currentMaxVelocity && velocityX > (currentMaxVelocity - 0.05f))
            velocityX = currentMaxVelocity;
    }
}

