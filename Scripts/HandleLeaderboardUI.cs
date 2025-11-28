using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class HandleLeaderboardUI : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform leaderboardParent;
    [SerializeField] private GameObject sessionCodeObj;
    [SerializeField] private GameObject clickToRevealObj;
    [SerializeField] private GameObject playerLeaderboardStatPrefab; // single instance of a player stat
    public InputActionReference inputKey;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        leaderboardParent.gameObject.SetActive(false);
        sessionCodeObj.gameObject.SetActive(false);

        base.OnNetworkSpawn();
    }

    private void Update()
    {
        if (!IsOwner || ServerManager.instance.isPracticeServer || ServerManager.instance.isTutorialServer)
            return;

        // sybau twin 
        if (inputKey.action.IsPressed() && !leaderboardParent.gameObject.activeInHierarchy && !HandleCursorSettings.instance.IsUIOn())
            SetUpLeaderboard();

        if (inputKey.action.WasReleasedThisFrame() && leaderboardParent.gameObject.activeInHierarchy)
            DisableLeaderboard();
    }

    private void SetUpLeaderboard()
    {
        // enable the cursor and disable cam movement
        HandleCursorSettings.instance.EnableCursor(true, false);

        // handle setting up the arrangment of the leaderboard dynamically
        // meaning that the owner's team will appear above the other team
        leaderboardParent.gameObject.SetActive(true);
        sessionCodeObj.gameObject.SetActive(true);

        // handle blue first, then red
        if (PlayerInfo.instance.currentTeam.Value.Equals("Blue"))
        {
            PlayerInfo[] players = FindObjectsByType<PlayerInfo>(FindObjectsSortMode.None);

            // blue team
            foreach (PlayerInfo player in players)
                if (player.currentTeam.Value.Equals("Blue") && player.playingObj.activeInHierarchy)
                    CreatePlayerStatsInstance(player);

            // red team
            foreach (PlayerInfo player in players)
                if (player.currentTeam.Value.Equals("Red") && player.playingObj.activeInHierarchy)
                    CreatePlayerStatsInstance(player);
        }

        // handle red first, then blue
        else
        {
            PlayerInfo[] players = FindObjectsByType<PlayerInfo>(FindObjectsSortMode.None);

            // red team
            foreach (PlayerInfo player in players)
                if (player.currentTeam.Value.Equals("Red"))
                    CreatePlayerStatsInstance(player);

            // blue team
            foreach (PlayerInfo player in players)
                if (player.currentTeam.Value.Equals("Blue"))
                    CreatePlayerStatsInstance(player);
        }
    }

    private void CreatePlayerStatsInstance(PlayerInfo player)
    {
        GameObject playerStatLeaderboard = Instantiate(playerLeaderboardStatPrefab, leaderboardParent);
        playerStatLeaderboard.GetComponent<HandlePlayerLeaderboardStats>().InitializePlayerStats(player);
    }

    private void DisableLeaderboard()
    {
        // destroy all playerStats
        // maybe in the future when a client disconnects
        // we manually remove that client's playerStat
        // instead of deleting everytime when a user disables the leaderboard
        // type shiii
        foreach (Transform playerStat in leaderboardParent)
            if (playerStat.GetComponent<HandlePlayerLeaderboardStats>())
                Destroy(playerStat.gameObject);

        clickToRevealObj.SetActive(true);

        leaderboardParent.gameObject.SetActive(false);
        sessionCodeObj.gameObject.SetActive(false);

        // disable the cursor and reenable camera
        HandleCursorSettings.instance.EnableCursor(false, true);
    }

    public void RevealLobbyCode() => clickToRevealObj.SetActive(false);
}
