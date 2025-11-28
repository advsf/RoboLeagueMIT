using UnityEngine;
using Unity.Netcode;

public class HandleHitbox : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool isKickHitbox;
    [SerializeField] private bool isSlideHitbox;
    [SerializeField] private float forwardForceMultiplier;
    [SerializeField] private float upwardForceMultiplier;
    [SerializeField] private float minLookUpSensitivty;

    [Header("References")]
    [SerializeField] private HandleKicking kickScript;
    [SerializeField] private Camera cam;
    [SerializeField] private Rigidbody rb;
    private BallSync ballSynchronizer;

    private bool canSlideKick = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!isSlideHitbox)
            return;

        if (other.gameObject.GetComponent<BallSync>())
        {
            ballSynchronizer = BallManager.instance.FindNearestBall(transform.position);

            // with the way this script works, only allow the isKickhitbox enabled script to handle this
            if (PlayerMovement.instance.isSliding && isSlideHitbox && canSlideKick)
            {
                canSlideKick = false;

                Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));

                // calculate based on the speed of the ball and player
                Vector3 force = forwardForceMultiplier * PlayerMovement.instance.GetPlayerMoveDirection();

                // if we are looking up at a certain point
                // then add upward force as well
                if (Vector3.Dot(ray.direction, Vector3.up) >= minLookUpSensitivty)
                    force += upwardForceMultiplier * ray.direction;

                var kickPayload = new BallSync.InputPayload
                {
                    Tick = NetworkManager.Singleton.ServerTime.Tick,
                    Force = force,
                    AngularImpulse = Vector3.zero,
                    SlideKick = true
                };

                ballSynchronizer.LocalKick(kickPayload, (int)NetworkManager.LocalClientId);

                Invoke(nameof(AllowSlideKick), PlayerMovement.instance.slideCooldown);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsOwner || isSlideHitbox)
            return;

        // if we are within kicking distance of the ball
        if (other.gameObject.layer == LayerMask.NameToLayer("Ball"))
            kickScript.SetCanKickOrHead(isKickHitbox, true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsOwner)
            return;

        kickScript.SetCanKickOrHead(isKickHitbox, false);
    }

    private void AllowSlideKick() => canSlideKick = true;
}
