using UnityEngine;
using Unity.Netcode;
using TMPro;

public class HandleScoreboardUI : NetworkBehaviour
{
    public static HandleScoreboardUI instance;

    [Header("References")]
    [SerializeField] private GameObject scoreboardUI;
    [SerializeField] private GameObject scoreInformationUI;
    [SerializeField] private TextMeshProUGUI startGameHelpText;
    [SerializeField] private TextMeshProUGUI blueTeamGoalAmount;
    [SerializeField] private TextMeshProUGUI redTeamGoalAmount;
    [SerializeField] private TextMeshProUGUI timer;
    [SerializeField] private TextMeshProUGUI scoreInformationText;

    private void Start()
    {
        instance = this;

        blueTeamGoalAmount.text = ServerManager.instance.blueTeamGoalCount.Value.ToString();
        redTeamGoalAmount.text = ServerManager.instance.redTeamGoalCount.Value.ToString();

        timer.text = "00:00";

        ServerManager.instance.blueTeamGoalCount.OnValueChanged += ChangeBlueTeamGoalCount;
        ServerManager.instance.redTeamGoalCount.OnValueChanged += ChangeRedTeamGoalCount;
        ServerManager.instance.matchTime.OnValueChanged += UpdateTimer;

        // dont show the clients the helper text "Press T To Start Game"
        if (!IsServer)
            EnableStartGameHelperTextUI(false);
    }

    public override void OnNetworkDespawn()
    {
        instance = null;

        ServerManager.instance.blueTeamGoalCount.OnValueChanged -= ChangeBlueTeamGoalCount;
        ServerManager.instance.redTeamGoalCount.OnValueChanged -= ChangeRedTeamGoalCount;
        ServerManager.instance.matchTime.OnValueChanged -= UpdateTimer;

        base.OnNetworkDespawn();
    }

    public void ChangeScoreboardInformationText(string text) => scoreInformationText.text = text;

    public void EnableScoreboardInformationUI(bool condition) => scoreInformationUI.SetActive(condition);

    public void EnableTimerUI(bool condition) => scoreboardUI.SetActive(condition);

    public void EnableStartGameHelperTextUI(bool condition) => startGameHelpText.enabled = condition;

    private void UpdateTimer(float previous, float current)
    {
        int minutes = Mathf.FloorToInt(current / 60); 
        int seconds = Mathf.FloorToInt(current % 60);

        timer.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void ChangeBlueTeamGoalCount(int previous, int current)
    {
        blueTeamGoalAmount.text = current.ToString();
    }

    private void ChangeRedTeamGoalCount(int previous, int current)
    {
        redTeamGoalAmount.text = current.ToString();
    }
}
