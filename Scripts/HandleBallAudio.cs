using UnityEngine;

public class HandleBallAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip ballBounceSound;

    [Header("Ball Bounce Settings")]
    [SerializeField] private float ballBounceMinimumYHeightThreshold;

    private float maxHeightBall;

    private void Update()
    {
        if (transform.position.y > maxHeightBall)
            maxHeightBall = transform.position.y;
    }

    private void PlayBallBounceAudio() => source.PlayOneShot(ballBounceSound);

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            if (maxHeightBall > ballBounceMinimumYHeightThreshold)
            {
                PlayBallBounceAudio();
                maxHeightBall = 0;
            }
        }
    }
}
