using UnityEngine;
using TMPro;
using System.Collections;

public class HandleTutorialObjectiveUI : MonoBehaviour
{
    public static HandleTutorialObjectiveUI instance;

    [Header("References")]
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private Animator animator;

    private int onEnableHash;
    private int onDisableHash;

    private int amountOfDetectorsPassed = 0;

    private void Start()
    {
        instance = this;

        onEnableHash = Animator.StringToHash("OnEnable");
        onDisableHash = Animator.StringToHash("OnDisable");
    }

    private void OnDisable()
    {
        instance = null;
    }

    public void EnableTutorialObjectiveUI()
    {
        animator.SetTrigger(onEnableHash);

        amountOfDetectorsPassed = 0;
        objectiveText.color = Color.white;

        UpdateTutorialObjectiveUI();
    }

    public IEnumerator DisableTutorialObjectiveUI()
    {
        objectiveText.color = Color.green;

        yield return new WaitForSeconds(1);

        animator.SetTrigger(onDisableHash);
    }

    public void UpdateTutorialObjectiveUI()
    {
        switch(TutorialManager.instance.currentTutorialStageNumber)
        {
            // movement
            case 0:
                objectiveText.text = $"Pass through the barriers ({amountOfDetectorsPassed}/5)";

                if (amountOfDetectorsPassed == 5)
                    StartCoroutine(DisableTutorialObjectiveUI());
                break;

            // dribbling
            case 1:
                objectiveText.text = $"Dribble through the barriers ({amountOfDetectorsPassed}/8)";

                if (amountOfDetectorsPassed == 8)
                    StartCoroutine(DisableTutorialObjectiveUI());
                break;
            
            // shooting
            case 2:
                objectiveText.text = $"Score ({amountOfDetectorsPassed}/8)";

                if (amountOfDetectorsPassed == 8)
                    StartCoroutine(DisableTutorialObjectiveUI());
                break;
            // power kick
            case 3:
                objectiveText.text = $"Score by powerkick ({amountOfDetectorsPassed}/3)";

                if (amountOfDetectorsPassed == 3)
                    StartCoroutine(DisableTutorialObjectiveUI());
                break;
            // speedster
            case 4:
                objectiveText.text = $"Use the speedster ability ({amountOfDetectorsPassed}/1)";

                if (amountOfDetectorsPassed == 1)
                    StartCoroutine(DisableTutorialObjectiveUI());
                break;
            // trap
            case 5:
                objectiveText.text = $"Trap the ball ({amountOfDetectorsPassed}/3)";

                if (amountOfDetectorsPassed == 3)
                    StartCoroutine(DisableTutorialObjectiveUI());
                break;
            // roulette
            case 6:
                objectiveText.text = $"Use the roulette ability ({amountOfDetectorsPassed}/1)";

                if (amountOfDetectorsPassed == 1)
                    StartCoroutine(DisableTutorialObjectiveUI());
                break;
            // kick
            case 7:
                objectiveText.text = $"Use the kick ability ({amountOfDetectorsPassed}/1)";

                if (amountOfDetectorsPassed == 1)
                    StartCoroutine(DisableTutorialObjectiveUI());
                break;
            // deflect
            case 8:
                objectiveText.text = $"Use the deflect ability ({amountOfDetectorsPassed}/1)";

                if (amountOfDetectorsPassed == 1)
                    StartCoroutine(DisableTutorialObjectiveUI());
                break;
            // goalkeeper dive
            case 9:
                objectiveText.text = $"Dive and deflect the ball ({amountOfDetectorsPassed}/3)";

                if (amountOfDetectorsPassed == 3)
                    StartCoroutine(DisableTutorialObjectiveUI());
                break;
            // goalkeeper catch, drop kick, roll
            case 10:
                if (amountOfDetectorsPassed == 0)
                    objectiveText.text = $"Catch the ball ({amountOfDetectorsPassed}/3)";
                else if (amountOfDetectorsPassed == 1)
                    objectiveText.text = $"Drop kick ({amountOfDetectorsPassed}/3)";
                else
                    objectiveText.text = $"Catch then roll the ball ({amountOfDetectorsPassed}/3)";

                if (amountOfDetectorsPassed == 3)
                    StartCoroutine(DisableTutorialObjectiveUI());

                break;
        }

        amountOfDetectorsPassed++;
    }
} 
