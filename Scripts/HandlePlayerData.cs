using System.Collections;
using UnityEngine;
public class HandlePlayerData : MonoBehaviour
{
    public static HandlePlayerData instance;

    [Header("Rank Images References")]
    [SerializeField] private Sprite[] rankSprites;

    public bool isPlayerMaxRank = false;
    public bool isPlayerLowestRank = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    private void OnEnable()
    {
        IncreaseRankEXP(0);

        FBPP.SetInt("RankIndex", Mathf.Clamp(FBPP.GetInt("RankIndex"), 0, rankSprites.Length - 1));

        isPlayerMaxRank = FBPP.GetInt("RankIndex") == rankSprites.Length - 1;
        isPlayerLowestRank = FBPP.GetInt("RankIndex") == 0;

        InitializeDatas();
    }

    private void InitializeDatas()
    {
        if (!FBPP.HasKey("Username"))
        {
            FBPP.SetString("Username", "Guest" + Random.Range(1, 200));
            FBPP.SetInt("Goals", 0);
            FBPP.SetInt("Assists", 0);
            FBPP.SetInt("Saves", 0);
            FBPP.SetInt("RankIndex", 0);
            FBPP.SetFloat("RankXP", 0);
        }
    }

    #region Player Username
    public string GetUsername()
    {
        return FBPP.GetString("Username");
    }

    #endregion

    #region Player Game Stats Data
    public void UpdateGoalsCount()
    {
        FBPP.SetInt("Goals", FBPP.GetInt("Goals") + 1);
        FBPP.Save();
    }

    public int GetGoalsCount()
    {
        return FBPP.GetInt("Goals");
    }

    public void UpdateAssistsCount()
    {
        FBPP.SetInt("Assists", FBPP.GetInt("Assists") + 1);
        FBPP.Save();
    }

    public int GetAssistsCount()
    {
        return FBPP.GetInt("Assists");
    }

    public void UpdateSavesCount()
    {
        FBPP.SetInt("Saves", FBPP.GetInt("Saves") + 1);
        FBPP.Save();
    }

    public int GetSavesCount()
    {
        return FBPP.GetInt("Saves");
    }

    #endregion

    #region Player Rank Data

    public Sprite GetRankSprite()
    {
        return rankSprites[FBPP.GetInt("RankIndex")];
    }

    public Sprite GetRankSprite(int index)
    {
        return rankSprites[index];
    }

    public void IncreaseRankEXP(float xp)
    {
        float newXP = FBPP.GetFloat("RankXP") + xp;
        int rankIndex = FBPP.GetInt("RankIndex");

        while (true)
        {
            if (isPlayerMaxRank)
            {
                // rank down
                if (newXP < 0)
                {
                    rankIndex = Mathf.Max(0, rankIndex - 1);
                    newXP = 100 + newXP;
                }

                else
                    newXP = Mathf.Clamp(newXP, 0, 100);
            }

            else if (isPlayerLowestRank)
            {
                // rank up
                if (newXP >= 100)
                {
                    rankIndex = Mathf.Min(rankSprites.Length - 1, rankIndex + 1);
                    newXP -= 100;
                }
                else
                    newXP = Mathf.Clamp(newXP, 0, 100);
            }

            else
            {
                if (newXP >= 100)
                {
                    newXP -= 100;
                    rankIndex++;

                    // stop if reached max
                    if (rankIndex >= rankSprites.Length - 1)
                    {
                        rankIndex = rankSprites.Length - 1;
                        newXP = Mathf.Clamp(newXP, 0, 100);
                        break;
                    }
                }
                else if (newXP < 0)
                {
                    // rank down and carry over
                    newXP = 100 + newXP;
                    rankIndex--;

                    // stop if reached lowest
                    if (rankIndex <= 0)
                    {
                        rankIndex = 0;
                        newXP = Mathf.Clamp(newXP, 0, 100);
                        break;
                    }
                }

                else
                    break;
            }

            // exit if XP is now no longer overflowing
            if (newXP >= 0 && newXP < 100 && rankIndex > 0 && rankIndex < rankSprites.Length - 1)
                break;
        }

        FBPP.SetInt("RankIndex", rankIndex);
        FBPP.SetFloat("RankXP", newXP);
        FBPP.Save();

        isPlayerMaxRank = rankIndex == rankSprites.Length - 1;
        isPlayerLowestRank = rankIndex == 0;

        // if we are in the main lobby
        if (ServerManager.instance == null)
            HandlePlayerStatsUI.instance.UpdateStatsUI();
    }

    #endregion
}
