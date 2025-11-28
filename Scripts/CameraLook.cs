using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class CameraLook : NetworkBehaviour
{
    public static CameraLook instance;

    [Header("Settings")]
    [SerializeField] private float sensMultiplier = 0.001f;
    [SerializeField] private float minYRot = -25f;
    [SerializeField] private float maxYRot = 80f;
    [SerializeField] private bool isPlayingCamera;

    [Header("Orbit & Collision")]
    [SerializeField] private LayerMask collisionLayers; 
    [SerializeField] private float collisionPadding = 0.2f; 
    private float cameraDistance;
    private float cameraYOffset;

    [Header("References")]
    [SerializeField] private Transform player;

    private float mouseX;
    private float mouseY;
    private float yRot;

    private bool canCamMove;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            canCamMove = true;
        }

        else
        {
            GetComponent<Camera>().enabled = false;
            GetComponent<AudioListener>().enabled = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
            return;

        if (instance == this)
            instance = null;

        base.OnNetworkDespawn();
    }

    private void OnEnable()
    {
        if (!IsOwner)
            return;

        // since we are using two camera system
        // everytime we switch, change the singleton pattern to reference this specific camera
        instance = this;

        HandleCursorSettings.instance.EnableCursor(false, true);
    }

    private void LateUpdate()
    {
        if (!IsOwner)
            return;

        HandleLook();
    }

    private void HandleLook()
    {
        // get user input if camera can be moved
        if (canCamMove)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            mouseX = mouseDelta.x * FBPP.GetInt("InvertHorizontalMouse");
            mouseY = mouseDelta.y * FBPP.GetInt("InvertVerticalMouse");

            float sensitivity = FBPP.GetFloat("Sensitivity") * sensMultiplier;

            if (isPlayingCamera)
            {
                if (mouseX != 0)
                    player.Rotate(Vector3.up * mouseX * sensitivity);

                yRot += -mouseY * sensitivity;
                yRot = Mathf.Clamp(yRot, minYRot, maxYRot);
            }
            else
            {
                player.Rotate(Vector3.up * mouseX * sensitivity);

                yRot += -mouseY * sensitivity;
                yRot = Mathf.Clamp(yRot, minYRot, maxYRot);
            }
        }

        // update camera position always
        if (isPlayingCamera)
        {
            cameraDistance = FBPP.GetFloat("CameraDistance");
            cameraYOffset = FBPP.GetFloat("CameraYOffset");

            Vector3 orbitCenter = player.position + new Vector3(0f, cameraYOffset, 0f);
            Quaternion rotation = Quaternion.Euler(yRot, player.eulerAngles.y, 0f);
            Vector3 desiredPosition = orbitCenter + (rotation * Vector3.back * cameraDistance);

            // collision detection
            Vector3 finalPosition = desiredPosition;
            if (Physics.Linecast(orbitCenter, desiredPosition, out RaycastHit hit, collisionLayers))
                finalPosition = hit.point + (hit.normal * collisionPadding);

            transform.position = finalPosition;
            transform.LookAt(orbitCenter);
        }

        // spectator camera
        else
            transform.localRotation = Quaternion.Euler(yRot, 0f, 0f);
    }
    public void EnableCamera(bool condition)
    {
        canCamMove = condition;
    }
}