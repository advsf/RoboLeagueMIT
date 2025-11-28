using UnityEngine;
using System.Collections;

/* tutorial stages:

0. movement(wasd, sprinting, sliding, dashing, and jumping)
1. dribbling
2. shooting(also tell user to flick it to add height or make it spin)
3. powerkick
4. speedster
5. trap
6. roulette
7. kick
8. deflect
9. goalkeeping (diving, catching, drop kicking, and rolling) 

 */
public class HandleTutorialPlayerDetectors : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject[] playerDetectors;
    private int currentPlayerDetectorIndex;

    [Header("Settings")]
    [SerializeField] private float stageTransitionDuration = 3;

    private void OnEnable()
    {
        currentPlayerDetectorIndex = 0;

        foreach (GameObject detector in playerDetectors)
            detector.SetActive(false);

        // enable the first one
        playerDetectors[currentPlayerDetectorIndex].SetActive(true);

        TutorialPlayerDetectorListener tutorialListener = playerDetectors[currentPlayerDetectorIndex].GetComponentInChildren<TutorialPlayerDetectorListener>();

        // activiate tutorial UI
        if (tutorialListener.shouldOpenBeginningTutorialUI)
            tutorialListener.OpenTutorialUI();
    }

    public void AdvanceToNextDetector(float delay)
    {
        StartCoroutine(HandleMovingToTheNextDetector(delay));
    }

    private IEnumerator HandleMovingToTheNextDetector(float delay)
    {
        SoundManager.instance.PlayTutorialPlayerDetectorHitSound();

        playerDetectors[currentPlayerDetectorIndex].SetActive(false);

        if (delay > 0)
            yield return new WaitForSeconds(delay);

        if (currentPlayerDetectorIndex + 1 < playerDetectors.Length)
        {
            currentPlayerDetectorIndex++;

            // activate the next one
            playerDetectors[currentPlayerDetectorIndex].SetActive(true);

            TutorialPlayerDetectorListener tutorialListener = playerDetectors[currentPlayerDetectorIndex].GetComponentInChildren<TutorialPlayerDetectorListener>();

            // activiate tutorial UI
            if (tutorialListener.shouldOpenBeginningTutorialUI)
                tutorialListener.OpenTutorialUI();
        }

        else
            Invoke(nameof(MoveToNextStage), stageTransitionDuration);

        yield return null;
    }

    private void MoveToNextStage()
    {
        TutorialManager.instance.MoveToNextTutorialStep();
    }

    // this is used for abilities that require interacting with the ball
    // so that we know that the player actually performed the move correctly
    // e.g. trap, roulette, kicking, and deflect
    public void MoveToNextDetector()
    {
        StartCoroutine(playerDetectors[currentPlayerDetectorIndex].GetComponentInChildren<TutorialPlayerDetectorListener>().PassToNextDectector());
    }
}
