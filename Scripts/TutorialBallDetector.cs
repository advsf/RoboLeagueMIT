using UnityEngine;

public class TutorialBallDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TutorialPlayerDetectorListener playerDetectorListener;
    [SerializeField] private float cooldown = 1;

    private bool isHit = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball") && !isHit)
        {
            playerDetectorListener.TeleportBallBack();
            isHit = true;

            Invoke(nameof(ResetHitCooldown), cooldown);
        }
    }

    private void ResetHitCooldown()
    {
        isHit = false;
    }
}
