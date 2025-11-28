using UnityEngine;
using TMPro; 

public class HandleVariousUIAnimationsInGame : MonoBehaviour
{
    public static HandleVariousUIAnimationsInGame instance;

    [Header("Goal Score Animation UI")]
    [SerializeField] private GameObject goalScoreUIObj;
    [SerializeField] private TextMeshProUGUI scoredByText;
    [SerializeField] private TextMeshProUGUI assistedByText;
    [SerializeField] private float goalScoreAnimationDuration = 5f;

    [Header("Ball References")]
    [SerializeField] private BallSync ballSync;

    private void Start()
    {
        instance = this;
        goalScoreUIObj.SetActive(false);
    }

    private void OnDisable()
    {
        instance = null;
    }

    public void PlayGoalScoreUIAnimation()
    {
        // reset the texts
        scoredByText.text = "";
        assistedByText.text = "";

        goalScoreUIObj.SetActive(true); // all that is needed to do since the base animation is the animation that we are looking to play

        scoredByText.text = $"Scored by {ballSync.scorerUsername.Value}";

        if (!ballSync.assisterUsername.Value.IsEmpty)
            assistedByText.text = $"Assisted by {ballSync.assisterUsername.Value}";
        else
            assistedByText.text = $"Assisted by none";

        Invoke(nameof(DisableGoalScoreUIAnimation), goalScoreAnimationDuration);
    }

    private void DisableGoalScoreUIAnimation() => goalScoreUIObj.SetActive(false);
}
