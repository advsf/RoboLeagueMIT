using UnityEngine;

public class HandleNetSounds : MonoBehaviour
{
    public static bool canPlayNetSound = true;

    [Header("Audio References")]
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip goalPostHitSoundEffect;
    [SerializeField] private AudioClip ballHitNetSoundEffect;

    [Header("Other References")]
    [SerializeField] private float netSoundCooldown = 0.15f;
    [SerializeField] private float minBallVelToTriggerNetSound = 35f;

    private void OnCollisionEnter(Collision collision)
    {
        bool isBallHit = collision.gameObject.layer.Equals(LayerMask.NameToLayer("Ball"));

        if (!isBallHit)
            return;

        Rigidbody ballRb = collision.gameObject.GetComponentInChildren<Rigidbody>();

        // post sound 
        if (name.Equals("Post"))
        {
            source.PlayOneShot(goalPostHitSoundEffect);
        }

        // net sound
        else if (name.Equals("Net") && ballRb.linearVelocity.magnitude > minBallVelToTriggerNetSound && canPlayNetSound)
        {
            canPlayNetSound = false;

            source.PlayOneShot(ballHitNetSoundEffect);
            Invoke(nameof(ResetNetSoundCooldown), netSoundCooldown);
        }
    }

    private void ResetNetSoundCooldown()
    {
        canPlayNetSound = true;
    }
}
