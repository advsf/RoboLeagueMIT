using UnityEngine;
using Unity.Collections;
using Unity.Netcode;

public class PlayerInfo : NetworkBehaviour
{
    public static PlayerInfo instance;

    public enum PlayerMode 
    {   
        Default, 
        Playing, 
        Spectating 
    }

    [Header("Playing Player References")]
    public NetworkVariable<FixedString64Bytes> username = new(
    "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString32Bytes> currentTeam = new(
           "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString32Bytes> currentPosition = new(
        "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> rankIndex = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<int> goals = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> assists = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> saves = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<int> ping = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<int> id = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<byte> CurrentPlayerMode = new(
        (byte)PlayerMode.Default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] private float pingSmoothingFactor = 0.1f;
    [SerializeField] private Rigidbody playingRb;
    public GameObject playingObj;

    [Header("Playing Player Material References")]
    [SerializeField] private Material bluePlayerMat;
    [SerializeField] private Material redPlayerMat;
    [SerializeField] private SkinnedMeshRenderer playerObjRenderer;

    [Header("Spectating Player References")]
    [SerializeField] private Rigidbody spectatingRb;
    public GameObject spectatingObj;

    [Header("Default Player References")]
    public GameObject defaultPlayerObj;

    [Header("Audio Source Reference")]
    [SerializeField] private GameObject audioSourcesObj;

    public float gamePlayDuration = 0;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        CurrentPlayerMode.OnValueChanged += OnPlayerModeChanged;

        // sync the material color
        if (!IsOwner && (PlayerMode) CurrentPlayerMode.Value == PlayerMode.Playing)
        {
            ChangePlayerMaterial(currentTeam.Value.ToString());
        }

        if (IsOwner)
        {
            // singleton
            instance = this;

            string localUsername = HandlePlayerData.instance.GetUsername();

            if (IsHost)
                username.Value = localUsername;
            else
                SetUsernameServerRpc(localUsername);

            // initialize rank
            rankIndex.Value = FBPP.GetInt("RankIndex");

            // set up listeners for when a goals stat increases
            goals.OnValueChanged += UpdateGoalsDataCount;
            assists.OnValueChanged += UpdateAssistsDataCount;
            saves.OnValueChanged += UpdateSavesDataCount;

            id.Value = (int) OwnerClientId;
        }
    }

    public override void OnNetworkDespawn()
    {
        CurrentPlayerMode.OnValueChanged -= OnPlayerModeChanged;

        base.OnNetworkDespawn();
    }

    private void Start()
    {
        if (!IsOwner)
            ApplyPlayerModeChange((PlayerMode)CurrentPlayerMode.Value);

        if (!IsOwner)
            return;

        ApplyPlayerModeChange(PlayerMode.Default);
        ChangePlayerMode(PlayerMode.Default);
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (ServerManager.instance != null)
            if (ServerManager.instance.didStartGame.Value)
                gamePlayDuration += Time.deltaTime;

        int rawPing = (int) NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.Singleton.NetworkConfig.NetworkTransport.ServerClientId);

        ping.Value = Mathf.RoundToInt(Mathf.Lerp(ping.Value, rawPing, pingSmoothingFactor));
    }

    public void ResetPlayerPosition(bool didConcede)
    {
        if (!IsOwner) 
            return;

        Transform spawnPoint = null;

        // if we conceded and we are CF
        if (didConcede && currentPosition.Value.Equals("CF"))
        {
            // find the spawn point at the moment we need it
            string concedeSpawnName = currentTeam.Value.ToString() + currentPosition.Value.ToString() + "SpawnStart";
            GameObject concedeSpawnObject = GameObject.Find(concedeSpawnName);

            spawnPoint = concedeSpawnObject.transform;
        }

        // if not CF or did not concede
        else
        {
            string regularSpawnName = currentTeam.Value.ToString() + currentPosition.Value.ToString() + "Spawn";
            GameObject regularSpawnObject = GameObject.Find(regularSpawnName);

            spawnPoint = regularSpawnObject.transform;
        }

        // move the player to the found spawn point
        playingRb.position = spawnPoint.position;
        playingRb.linearVelocity = Vector3.zero;
    }

    public void StartAsPlayingPlayer(bool didConcede, PlayerDataTypes.Team team, PlayerDataTypes.Position position)
    {
        if (!IsOwner) 
            return;

        // change the layer of the player
        if (team == PlayerDataTypes.Team.Blue)
        {
            foreach (Transform child in transform)
                child.gameObject.layer = LayerMask.NameToLayer("BluePlayer");
        }

        else
        {
            foreach (Transform child in transform)
                child.gameObject.layer = LayerMask.NameToLayer("RedPlayer");
        }

        // set it to playing
        ApplyPlayerModeChange(PlayerMode.Playing);
        ChangePlayerMode(PlayerMode.Playing);

        // handle the material color
        ChangePlayerMaterial(team.ToString());
        SyncPlayerMaterial(team.ToString());

        // disable the cursor
        HandleCursorSettings.instance.EnableCursor(false);

        Transform spawnPoint = null;

        transform.GetComponentInChildren<HandleAbilities>().SetPositionAbilities(position.ToString());

        // if we conceded and we are CF
        if (didConcede && position.Equals("CF"))
        {
            // find the spawn point at the moment we need it
            string concedeSpawnName = team.ToString() + position.ToString() + "SpawnStart";
            GameObject concedeSpawnObject = GameObject.Find(concedeSpawnName);

            spawnPoint = concedeSpawnObject.transform;
        }

        // if not CF or did not concede
        else
        {
            string regularSpawnName = team.ToString() + position.ToString() + "Spawn";
            GameObject regularSpawnObject = GameObject.Find(regularSpawnName);

            spawnPoint = regularSpawnObject.transform;
        }

        // move the player to the found spawn point
        playingRb.position = spawnPoint.position;
        playingRb.linearVelocity = Vector3.zero;
    }


    public void StartTutorialPlayer(Transform spawnPoint, PlayerDataTypes.Team team, PlayerDataTypes.Position position, bool hasAbility)
    {
        if (!IsOwner)
            return;

        // change the layer of the player
        if (team == PlayerDataTypes.Team.Blue)
        {
            foreach (Transform child in transform)
                child.gameObject.layer = LayerMask.NameToLayer("BluePlayer");
        }

        else
        {
            foreach (Transform child in transform)
                child.gameObject.layer = LayerMask.NameToLayer("RedPlayer");
        }

        // set it to playing
        ApplyPlayerModeChange(PlayerMode.Playing);
        ChangePlayerMode(PlayerMode.Playing);

        // handle the material color
        ChangePlayerMaterial(team.ToString());
        SyncPlayerMaterial(team.ToString());

        // "" will make the abilities set to "None"
        transform.GetComponentInChildren<HandleAbilities>().SetPositionAbilities(hasAbility ? position.ToString() : "");

        // move the player to the set spawnPoint
        playingRb.position = spawnPoint.position;
        playingRb.linearVelocity = Vector3.zero;
    }

    public void SetTeamAndPositionAndUsername(PlayerDataTypes.Team team, PlayerDataTypes.Position position, string username)
    {
        currentTeam.Value = new(team.ToString());
        currentPosition.Value = new(position.ToString());
        this.username.Value = new(username);
    }

    private void OnPlayerModeChanged(byte previousMode, byte newMode)
    {
        ApplyPlayerModeChange((PlayerMode)newMode);
    }

    public void ChangeIntoSpectatorMode()
    {
        if (!IsOwner)
            return;

        spectatingRb.position = new(0, 20, 0);

        // remove their spot, if any
        ServerManager.instance.RequestSpectateServerRpc();

        RequestPlayerModeChangeServerRpc((byte)PlayerMode.Spectating);
    }

    public void ChangePlayerMode(PlayerMode newMode)
    {
        if (!IsOwner) 
            return;

        RequestPlayerModeChangeServerRpc((byte)newMode);
    }

    [ServerRpc]
    private void RequestPlayerModeChangeServerRpc(byte newMode)
    {
        CurrentPlayerMode.Value = newMode;
    }

    private void ApplyPlayerModeChange(PlayerMode mode)
    {
        switch (mode)
        {
            case PlayerMode.Playing:
                playingObj.SetActive(true);
                spectatingObj.SetActive(false);
                defaultPlayerObj.SetActive(false);
                break;

            case PlayerMode.Spectating:
                playingObj.SetActive(false);
                spectatingObj.SetActive(true);
                defaultPlayerObj.SetActive(false);
                break;

            case PlayerMode.Default:
            default:
                playingObj.SetActive(false);
                spectatingObj.SetActive(false);
                defaultPlayerObj.SetActive(true);
                break;
        }

        MoveAudioSources(mode);
    }

    private void MoveAudioSources(PlayerMode mode)
    {
        switch (mode)
        {
            case PlayerMode.Playing:
                audioSourcesObj.transform.parent = playingObj.transform.GetChild(0).transform;
                audioSourcesObj.transform.position = Vector3.zero;
                break;

            case PlayerMode.Spectating:
                audioSourcesObj.transform.parent = spectatingObj.transform.GetChild(0).transform;
                audioSourcesObj.transform.position = Vector3.zero;
                break;

            case PlayerMode.Default:
            default:
                break;
        }
    }

    private void SyncPlayerMaterial(string team)
    {
        if (IsHost)
            SyncPlayerMaterialClientRpc(team);
        else
            SyncPlayerMaterialServerRpc(team);
    }

    private void ChangePlayerMaterial(string team)
    {
        if (team.ToString().Equals("Blue"))
            playerObjRenderer.material = bluePlayerMat;
        else
            playerObjRenderer.material = redPlayerMat;
    }

    public Transform GetCurrentActiveObjectTransform()
    {
        if (playingObj.activeInHierarchy)
            return playingObj.transform;
        else if (spectatingObj.activeInHierarchy)
            return spectatingObj.transform;
        else
            return defaultPlayerObj.transform;
    }

    [ServerRpc]
    private void SyncPlayerMaterialServerRpc(string team) => SyncPlayerMaterialClientRpc(team);

    [ClientRpc]
    private void SyncPlayerMaterialClientRpc(string team) => ChangePlayerMaterial(team);

    [ServerRpc]
    private void SetUsernameServerRpc(string name)
    {
        username.Value = name;
    }

    #region Network variable listeners

    private void UpdateGoalsDataCount(int previousValue, int newValue)
    {
        // must have at least 4 for the data to save
        // i know that this system can be easily exploited
        // but it's the only one that i can use at the moment
        if (ServerManager.instance.spawnedPlayerCount.Value < 3 || !ServerManager.instance.isPracticeServer || !ServerManager.instance.isTutorialServer)
            return;

        FBPP.SetInt("Goals", FBPP.GetInt("Goals") + 1);
    }

    private void UpdateAssistsDataCount(int previousValue, int newValue)
    {
        // must have at least 4 for the data to save
        if (ServerManager.instance.spawnedPlayerCount.Value < 3 || !ServerManager.instance.isPracticeServer || !ServerManager.instance.isTutorialServer)
            return;

        FBPP.SetInt("Assists", FBPP.GetInt("Assists") + 1);
    }

    private void UpdateSavesDataCount(int previousValue, int newValue)
    {
        // must have at least 4 for the data to save
        if (ServerManager.instance.spawnedPlayerCount.Value < 3 || !ServerManager.instance.isPracticeServer || !ServerManager.instance.isTutorialServer)
            return;

        FBPP.SetInt("Saves", FBPP.GetInt("Saves") + 1);
    }

    #endregion
}
