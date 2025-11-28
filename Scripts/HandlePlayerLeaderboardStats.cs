using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HandlePlayerLeaderboardStats : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image rankImage;
    [SerializeField] private Image rankShadowImage;
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private TextMeshProUGUI goalsText;
    [SerializeField] private TextMeshProUGUI assistsText;
    [SerializeField] private TextMeshProUGUI savesText;
    [SerializeField] private TextMeshProUGUI positionText;
    [SerializeField] private TextMeshProUGUI pingText;
    [SerializeField] private Image[] backgrounds;

    [Header("Alpha Background Settings")]
    [SerializeField] private float lowerAlphaBackground;
    [SerializeField] private float higherAlphaBackground;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 1;

    private PlayerInfo playerInfo;
    private float updateTimer;

    private void OnEnable()
    {
        // skip the cooldown
        updateTimer = updateInterval + 2;
    }

    public void InitializePlayerStats(PlayerInfo playerInfo)
    {
        this.playerInfo = playerInfo;

        // just to make sure
        transform.position = Vector3.zero;
    }

    private void Update()
    {
        // dont update every frame, instead update every 1 seconds or whatever the updateInterval is set to
        updateTimer += Time.deltaTime;

        if (updateTimer < updateInterval) 
            return;

        // reset the timer
        updateTimer = 0f;

        UpdateUI();
    }

    private void UpdateUI()
    {
        // handle background
        for (int i = 0; i < backgrounds.Length; i++)
        {
            // when the i is even
            // increase the alpha for the background
            // as a design
            if (playerInfo.currentTeam.Value.Equals("Blue"))
                backgrounds[i].color = new(0.3529412f, 0.5921569f, 0.9607843f, i % 2 == 0 ? higherAlphaBackground : lowerAlphaBackground); // blue
            else
                backgrounds[i].color = new(0.8018868f, 0.2458615f, 0.2517279f, i % 2 == 0 ? higherAlphaBackground : lowerAlphaBackground); // red
        }

        // handle the rank image
        rankImage.enabled = true;
        rankImage.sprite = HandlePlayerData.instance.GetRankSprite(playerInfo.rankIndex.Value);

        rankShadowImage.enabled = true;
        rankShadowImage.sprite = HandlePlayerData.instance.GetRankSprite(playerInfo.rankIndex.Value);

        // handle the texts
        // i know it's really bad updating this every set interval
        // but we gotta do it cause im too lazy to think of another solution with events (which might not even work due to how the network system is set up)
        usernameText.text = playerInfo.username.Value.ToString();
        goalsText.text = playerInfo.goals.Value.ToString();
        assistsText.text = playerInfo.assists.Value.ToString();
        savesText.text = playerInfo.saves.Value.ToString();
        positionText.text = playerInfo.currentPosition.Value.ToString();
        pingText.text = playerInfo.ping.Value.ToString();

        if (playerInfo.id.Value == PlayerInfo.instance.id.Value)
            ChangeTextColor(new(0.9031128f, 1, 0.25f, 1f));
        else
            ChangeTextColor(new(1, 1, 1, 1));
    }

    private void ChangeTextColor(Color color)
    {
        usernameText.color = color;
        goalsText.color = color;
        assistsText.color = color;
        savesText.color = color;
        positionText.color = color;
        pingText.color = color;
    }
}
