using UnityEngine;

public class HandleOpeningSceneAnimation : MonoBehaviour
{
    public static bool hasPlayed = false;

    [SerializeField ] private Animator animator;

    private void Start()
    {
        // prevents this animation from being played again when the scene reloads
        // ensures that we only play the animation when the game first loads
        if (hasPlayed)
            gameObject.SetActive(false);
        else
            hasPlayed = true;
    }
}
