using UnityEngine;

public class HandleTransitions : MonoBehaviour
{
    public static HandleTransitions instance;

    [SerializeField] private Animator animator;

    private int fadeInHash;
    private int fadeOutHash;

    private void Start()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        else
        {
            Destroy(gameObject);
        }

        fadeInHash = Animator.StringToHash("FadeIn");
        fadeOutHash = Animator.StringToHash("FadeOut");
    }

    public void PlayFadeInTransition()
    {
        animator.SetTrigger("FadeIn");
    }

    public void PlayFadeOutTransition()
    {
        animator.SetTrigger(fadeOutHash);
    }

    public void PlayFadeInMusic()
    {
        StartCoroutine(HandleLobbySound.instance.FadeInMusic(1.25f, 3));
    }
}
