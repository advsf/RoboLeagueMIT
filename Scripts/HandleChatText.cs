using UnityEngine;

public class HandleChatText : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private int activateHash;

    private void Start()
    {
        activateHash = Animator.StringToHash("onActivate");
    }

    public void DeleteChatAfterDuration(float time)
    {
        Invoke(nameof(PlayFadeOutAnimation), time - 1);
        Invoke(nameof(DeleteText), time);
    }

    private void PlayFadeOutAnimation() => animator.SetTrigger(activateHash);

    private void DeleteText() => Destroy(gameObject);
}
