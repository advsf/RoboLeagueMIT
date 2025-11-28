using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class HandleSpectatingMovement : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float flySpeed = 12f;
    [SerializeField] private float slowFlySpeed = 5f;

    [Header("References")]
    [SerializeField] private Rigidbody rb;
    public InputActionReference moveAction;
    public InputActionReference sprintAction;
    public InputActionReference jumpAction; // go up
    public InputActionReference slideAction; // go down

    private Vector2 inputDirection;
    private Vector3 moveDirection;

    private float desiredFlySpeed;
    private void OnEnable()
    {
        moveAction.action.Enable();
        sprintAction.action.Enable();
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        GatherInput();
        ControlSpeed();
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
            return;

        MoveSpectator();
        HandleCountermovement();
    }

    private void GatherInput()
    {
        // calculate the move direction based on the user input
        inputDirection = moveAction.action.ReadValue<Vector2>();
    }

    private void ControlSpeed()
    {
        desiredFlySpeed = sprintAction.action.ReadValue<float>() > 0 ? flySpeed : slowFlySpeed;
    }

    private void MoveSpectator()
    {
        // multiply the desired speed by 10 so that we can handle countermovement more effectively

        // move
        moveDirection = transform.forward * inputDirection.y + transform.right * inputDirection.x;
        rb.AddForce(10 * desiredFlySpeed * moveDirection.normalized, ForceMode.Force);

        // move upwards
        if (jumpAction.action.IsPressed())
            rb.AddForce(10 * desiredFlySpeed * Vector3.up, ForceMode.Force);

        // move downwards
        if (slideAction.action.IsPressed())
            rb.AddForce(10 * desiredFlySpeed * Vector3.down, ForceMode.Force);

        // stop moving if there's no more inputs
        if (inputDirection == Vector2.zero)
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, new(0f, 0f, 0f), Time.deltaTime * 10f);
    }

    private void HandleCountermovement()
    {
        Vector3 flatVel = new(rb.linearVelocity.x, rb.linearVelocity.y, rb.linearVelocity.z);

        // limit velocity if needed
        if (flatVel.magnitude > desiredFlySpeed)
        {
            Vector3 limitedVel = flatVel.normalized * desiredFlySpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, limitedVel.y, limitedVel.z);
        }
    }
}

