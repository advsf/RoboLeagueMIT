using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HandlePlayerStatsUI : MonoBehaviour
{
    public static HandlePlayerStatsUI instance;

    [Header("References")]
    [SerializeField] private Image rankImage;
    [SerializeField] private Image rankImageShadow;
    [SerializeField] private Slider rankBarSlider;
    [SerializeField] private TMP_InputField usernameInputField;
    [SerializeField] private TextMeshProUGUI rankXPText;
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private TextMeshProUGUI gameStatsText;

    [Header("UI Bar Settings")]
    [SerializeField] private float xpBarFillDuration = 1.5f;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    private void OnEnable()
    {
        StartCoroutine(InitializeUIAndAnimate());
    }

    private IEnumerator InitializeUIAndAnimate()
    {
        yield return null;

        usernameInputField.text = HandlePlayerData.instance.GetUsername();
        UpdateStatsUI(); 

        rankBarSlider.value = 0;

        yield return new WaitForSeconds(3);

        StartCoroutine(FillUpXPBar());
    }

    #region Username

    public void SetUsername(string newUsername)
    {
        FBPP.SetString("Username", newUsername);
        FBPP.Save();

        UpdateStatsUI();

        usernameInputField.text = newUsername;
    }

    public void SetUsernameThroughUI()
    {
        FBPP.SetString("Username", usernameInputField.text);
        FBPP.Save();

        UpdateStatsUI();

        usernameInputField.text = usernameInputField.text;
    }

    #endregion

    #region Player Stats UI
    public void UpdateStatsUI()
    {
        float currentXP = FBPP.GetFloat("RankXP");

        // rank image
        rankImage.enabled = true;
        rankImage.sprite = HandlePlayerData.instance.GetRankSprite();

        // rank shadow image
        rankImageShadow.enabled = true;
        rankImageShadow.sprite = HandlePlayerData.instance.GetRankSprite();

        // rank information
        rankBarSlider.value = FBPP.GetFloat("RankXP") / 100;

        if (HandlePlayerData.instance.isPlayerMaxRank)
            rankXPText.text = $"{currentXP}/100 LEGEND XP";

        else
            rankXPText.text = $"{currentXP}/100 XP AWAY FROM {HandlePlayerData.instance.GetRankSprite(FBPP.GetInt("RankIndex") + 1).name}";

        // username & game stat
        usernameText.text = $"Username | {HandlePlayerData.instance.GetUsername()}";
        gameStatsText.text = $"Goals: {HandlePlayerData.instance.GetGoalsCount()} | Assists: {HandlePlayerData.instance.GetAssistsCount()} | Saves: {HandlePlayerData.instance.GetSavesCount()}";
    }

    private IEnumerator FillUpXPBar()
    {
        rankBarSlider.value = 0; 
        float initialVal = 0; 
        float targetVal = FBPP.GetFloat("RankXP") / 100f;
        float time = 0f;

        while (time < xpBarFillDuration)
        {
            time += Time.deltaTime;
            rankBarSlider.value = Mathf.Lerp(initialVal, targetVal, time / xpBarFillDuration);

            Debug.Log("working");
            yield return null;
        }

        rankBarSlider.value = targetVal;
    }

    #endregion
}
