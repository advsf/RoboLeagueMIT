using UnityEngine;
using UnityEngine.InputSystem;
using System;
using Unity.Netcode;
using System.Collections;

[Serializable]
public struct MoveSpeeds
{
    public float forwardSpeed;
    public float sidewaysSpeed;
    public float backwardSpeed;
}

public class PlayerMovement : NetworkBehaviour
{
    public static PlayerMovement instance { get; private set; }

    [Header("Movement Settings")]
    [SerializeField] private float counterMovementMultiplier;
    [SerializeField] private float accelerationSmoothing = 8f;
    [SerializeField] private float decelerationSmoothing = 12f;
    public MoveSpeeds walkSpeeds = new() { forwardSpeed = 5f, sidewaysSpeed = 4f, backwardSpeed = 3f };
    public MoveSpeeds sprintSpeeds = new() { forwardSpeed = 10f, sidewaysSpeed = 8f, backwardSpeed = 6f };
    public bool canPlayerMove = true;
    private bool canSprint = true;

    [Header("Jumping Settings")]
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpDownForce; // acts as gravity to push the player down
    [SerializeField] private float jumpStaminaDecrease;
    public InputActionReference jump;
    private bool canJump = true;

    [Header("Sliding Settings")]
    [SerializeField] private float slideForce;
    [SerializeField] private float slideImpulseForceMultiplier;
    [SerializeField] private float slideDownForce; // acts as gravity to push the player down
    [SerializeField] private float slideMaxDuration;
    [SerializeField] private float slideStaminaDecrease;
    public InputActionReference slide;
    public bool canSlide = true;
    public bool isSliding = false;
    public float slideCooldown;
    private float startSlideTime; // used to count how long the user has been sliding for

    [Header("Dash Settings")]
    [SerializeField] private float dashForce;
    [SerializeField] private float dashStaminaDecrease;
    public InputActionReference dash;
    public float dashCooldown;
    public bool canDash = true;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float playerHeight;
    private bool isGrounded;

    [Header("Stamina Settings")]
    [SerializeField] private float staminaIncreaseFactor;
    [SerializeField] private float staminaDecreaseFactor;
    [SerializeField] private float staminaCooldown = 3.0f;
    public float currentStamina;
    public float maxStamina;
    private float previousStamina;

    // delegates
    public static event Action OnStaminaChanged;
    public static event Action<float> onStaminaChanged;

    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform cam;
    [SerializeField] private AnimationStateController animator;
    [SerializeField] private HandleAbilityCooldown abilityUI;
    [SerializeField] private HandleKicking kicking;
    [SerializeField] private HandleGoalkeeperAsPlayer gkPlayer;
    public InputActionReference move;
    public InputActionReference sprint;
    private Speedster speedsterAbility;

    private Vector2 inputDirection2D;
    private Vector3 moveDirection;
    private float desiredSpeed = 0;
    private float currentSpeed = 0f;

    // mainly for animations
    public bool IsWalking { get => inputDirection2D != Vector2.zero && !isSliding; }
    public bool IsSprinting { get => sprint.action.ReadValue<float>() > 0 && canSprint && IsWalking; }
    public bool IsWalkingForward { get => inputDirection2D.y > 0 && !isSliding; }
    public bool IsWalkingBackwards { get => inputDirection2D.y < 0 && !isSliding; }
    public bool IsStrafingLeft { get => inputDirection2D.x < 0 && !isSliding; }
    public bool IsStrafingRight { get => inputDirection2D.x > 0 && !isSliding; }

    public bool IsOnGround { get => isGrounded; }

    public bool isStaminaRecharging = false;

    // mainly for boolean checks
    public bool isWalking { get => IsWalking && !IsSprinting; }
    public bool isSprinting { get => IsWalking && IsSprinting; }

    public bool isMovementDisabled = false;

    public override void OnNetworkSpawn()
    {
        isMovementDisabled = false;

        if (!IsOwner)
            return;

        instance = this;

        InitializeSettings();

        move.action.Enable();
        sprint.action.Enable();
    }

    private void OnEnable()
    {
        if (!IsOwner) 
            return;

        move.action.Enable();
        sprint.action.Enable();
    }

    private void OnDisable()
    {
        if (!IsOwner) 
            return;

        move.action.Disable();
        sprint.action.Disable();
    }

    private void Update()
    {
        if (!IsOwner) return;

        GatherInput();
        SpeedControl();
        HandleStamina();

        isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 2, groundMask);

        if (isMovementDisabled || HandleCursorSettings.instance.IsUIOn()) return;

        if (!isStaminaRecharging)
        {
            if (jump.action.IsPressed() && canJump && isGrounded)
                Jump();

            if (inputDirection2D != Vector2.zero)
            {
                if (slide.action.ReadValue<float>() > 0 && canSlide && isGrounded)
                    StartCoroutine(Slide());

                if (dash.action.IsPressed() && canDash && isGrounded)
                    Dash();
            }
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !canPlayerMove) return;

        if (!isGrounded)
            rb.AddForce(Vector3.down * jumpDownForce, ForceMode.Impulse);

        HandleCountermovement();

        if (isMovementDisabled || HandleCursorSettings.instance.IsUIOn()) return;

        moveDirection = transform.forward * inputDirection2D.y + transform.right * inputDirection2D.x;

        MoveSpeeds activeSpeeds = IsSprinting && canSprint ? sprintSpeeds : walkSpeeds;

        float forwardComponent = inputDirection2D.y >= 0 ? inputDirection2D.y * activeSpeeds.forwardSpeed : inputDirection2D.y * activeSpeeds.backwardSpeed;
        float sidewaysComponent = inputDirection2D.x * activeSpeeds.sidewaysSpeed;

        Vector2 targetLocalVelocity = new Vector2(sidewaysComponent, forwardComponent);
        desiredSpeed = targetLocalVelocity.magnitude;

        if (!isSliding)
        {
            if (inputDirection2D != Vector2.zero)
                currentSpeed = Mathf.Lerp(currentSpeed, desiredSpeed, accelerationSmoothing * Time.fixedDeltaTime);
            else
                currentSpeed = Mathf.Lerp(currentSpeed, 0f, decelerationSmoothing * Time.fixedDeltaTime);
            rb.AddForce(moveDirection.normalized * currentSpeed, ForceMode.Impulse);
        }
    }

    private void InitializeSettings()
    {
        currentStamina = maxStamina;
    }

    private void GatherInput()
    {
        inputDirection2D = move.action.ReadValue<Vector2>();
    }


    private IEnumerator Slide()
    {
        canSlide = false;
        isSliding = true;
        animator.PlaySlideAnimation();
        SoundManager.instance.PlaySlideSoundEffect(slideMaxDuration);

        float slideTimer = 0f;
        Vector3 slideDirection = moveDirection.normalized;

        if (slideDirection == Vector3.zero)
            slideDirection = rb.linearVelocity.normalized;

        float startVelocity = rb.linearVelocity.magnitude;
        rb.AddForce(slideImpulseForceMultiplier * startVelocity * slideDirection, ForceMode.Impulse);

        while (slideTimer < slideMaxDuration)
        {
            if (currentStamina < 1) break;

            float slideProgress = slideTimer / slideMaxDuration;
            float currentSlideForce = Mathf.Lerp(startVelocity * slideForce, 0f, slideProgress * slideProgress);

            rb.AddForce(slideDirection * currentSlideForce, ForceMode.Force);
            rb.AddForce(Vector3.down * slideDownForce, ForceMode.Force);
            ChangeStamina(slideStaminaDecrease * Time.fixedDeltaTime);

            yield return new WaitForFixedUpdate();
            slideTimer += Time.fixedDeltaTime;
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        animator.StopSlideAnimation();
        isSliding = false;

        abilityUI.StartCooldown("Slide", slideCooldown);

        Invoke(nameof(HandleSlideCooldown), slideCooldown);
    }

    private void Jump()
    {
        canJump = false;

        ChangeStamina(jumpStaminaDecrease);

        animator.PlayJumpAnimation();

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        rb.AddForce(jumpForce * Vector3.up, ForceMode.Impulse);

        Invoke(nameof(AllowUserToJumpAgain), jumpCooldown);

    }

    private void Dash()
    {
        canDash = false;

        ChangeStamina(dashStaminaDecrease);
        SoundManager.instance.PlayDashSoundEffect();
        animator.PlayDashAnimation();

        rb.AddForce(moveDirection * dashForce, ForceMode.Impulse);

        abilityUI.StartCooldown("Dash", dashCooldown);

        Invoke(nameof(HandleDashCooldown), dashCooldown);
    }

    private void HandleSlideCooldown() => canSlide = true;
    private void HandleDashCooldown() => canDash = true;
    private void AllowUserToJumpAgain() => canJump = true;

    private void SpeedControl()
    {
        Vector3 horizontalVelocity = new (rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (horizontalVelocity.magnitude > desiredSpeed)
        {
            Vector3 targetVelocity = horizontalVelocity.normalized * desiredSpeed +
                                     new Vector3(0f, rb.linearVelocity.y, 0f);

            if (gkPlayer.isDiving)
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, rb.linearVelocity.normalized * desiredSpeed, Time.fixedDeltaTime * 35f);
            else
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 35f);
        }
    }

    private void HandleCountermovement()
    {
        if (!isGrounded) 
            return;

        Vector3 desiredVelocity = Vector3.zero; 
        Vector3 velocityDifference = desiredVelocity - rb.linearVelocity;
        velocityDifference.y = 0.0f;

        if (inputDirection2D == Vector2.zero)
            rb.AddForce(velocityDifference * counterMovementMultiplier, ForceMode.Acceleration);
    }

    private void HandleStamina()
    {
        if (currentStamina <= 0.0f)
        {
            canSprint = false;
            isStaminaRecharging = true;
            Invoke(nameof(AllowUserToSprintAgain), staminaCooldown);
        }

        if (IsSprinting && canSprint)
            currentStamina -= staminaDecreaseFactor * Time.deltaTime;
        else if (!isSliding && !kicking.isShooting && !kicking.isDribbling)
            currentStamina += staminaIncreaseFactor * Time.deltaTime;

        currentStamina = Mathf.Clamp(currentStamina, 0.0f, maxStamina);

        if (previousStamina != currentStamina)
            onStaminaChanged?.Invoke(currentStamina);

        previousStamina = currentStamina;
    }

    public void IncreaseSpeedForDuration(float speedIncreaseAmount, float duration, Speedster speedsterScript)
    {
        walkSpeeds.forwardSpeed += speedIncreaseAmount;
        walkSpeeds.sidewaysSpeed += speedIncreaseAmount;
        walkSpeeds.backwardSpeed += speedIncreaseAmount;

        sprintSpeeds.forwardSpeed += speedIncreaseAmount;
        sprintSpeeds.sidewaysSpeed += speedIncreaseAmount;
        sprintSpeeds.backwardSpeed += speedIncreaseAmount;

        speedsterAbility = speedsterScript;
        StartCoroutine(RevertSpeedAfterSpeedBoost(speedIncreaseAmount, duration));
    }


    public void IncreaseSpeedForDuration(float speedIncreaseAmount, float duration)
    {
        walkSpeeds.forwardSpeed += speedIncreaseAmount;
        walkSpeeds.sidewaysSpeed += speedIncreaseAmount;
        walkSpeeds.backwardSpeed += speedIncreaseAmount;

        sprintSpeeds.forwardSpeed += speedIncreaseAmount;
        sprintSpeeds.sidewaysSpeed += speedIncreaseAmount;
        sprintSpeeds.backwardSpeed += speedIncreaseAmount;

        StartCoroutine(RevertSpeedAfterNonAbilitySpeedBoost(speedIncreaseAmount, duration));
    }


    private IEnumerator RevertSpeedAfterNonAbilitySpeedBoost(float speedDecreaseAmount, float duration)
    {
        yield return new WaitForSeconds(duration);
        walkSpeeds.forwardSpeed -= speedDecreaseAmount;
        walkSpeeds.sidewaysSpeed -= speedDecreaseAmount;
        walkSpeeds.backwardSpeed -= speedDecreaseAmount;

        sprintSpeeds.forwardSpeed -= speedDecreaseAmount;
        sprintSpeeds.sidewaysSpeed -= speedDecreaseAmount;
        sprintSpeeds.backwardSpeed -= speedDecreaseAmount;
    }


    private IEnumerator RevertSpeedAfterSpeedBoost(float speedDecreaseAmount, float duration)
    {
        yield return new WaitForSeconds(duration);
        walkSpeeds.forwardSpeed -= speedDecreaseAmount;
        walkSpeeds.sidewaysSpeed -= speedDecreaseAmount;
        walkSpeeds.backwardSpeed -= speedDecreaseAmount;

        sprintSpeeds.forwardSpeed -= speedDecreaseAmount;
        sprintSpeeds.sidewaysSpeed -= speedDecreaseAmount;
        sprintSpeeds.backwardSpeed -= speedDecreaseAmount;

        HandleAbilities.instance.TriggerCooldown(speedsterAbility);
    }

    public void SetSpeed(float desiredVelocity) => rb.linearVelocity = desiredVelocity * moveDirection.normalized;

    public void ChangeStamina(float change)
    {
        currentStamina += change;
        currentStamina = Mathf.Clamp(currentStamina, 0.0f, maxStamina);
        onStaminaChanged?.Invoke(currentStamina);
        previousStamina = currentStamina;
    }

    private void AllowUserToSprintAgain()
    {
        canSprint = true;
        isStaminaRecharging = false;
    }

    public void TeleportPlayerToPosition(Vector3 pos)
    {
        rb.linearVelocity = Vector3.zero;
        rb.position = pos;
    }

    public void RotatePlayer(Vector3 rot)
    {
        transform.rotation = Quaternion.Euler(rot);
    }

    public void DisableMovement(bool condition) => isMovementDisabled = condition;
    public Vector3 GetPlayerMoveDirection() => moveDirection;
}