using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class HandleChatbox : NetworkBehaviour
{
    public static HandleChatbox instance;

    [Header("Input")]
    public InputActionReference enableChatAction;
    public InputActionReference changeChatOptionAction;

    [Header("Prefab References")]
    [SerializeField] private GameObject openChatText; // to be put in the scrollview with the chat being opened
    [SerializeField] private GameObject closedChatText; // similar to valorant, when the chat isnt opened show the message without the inputfield

    [Header("References")]
    [SerializeField] private GameObject quickChatObj;
    [SerializeField] private GameObject emoteChatObj;
    [SerializeField] private GameObject chatBoxObj;
    [SerializeField] private GameObject closedChat; // when the chat isnt opened
    [SerializeField] private GameObject openedChat; // when the chat is opened
    [SerializeField] private Transform openChatParent;
    [SerializeField] private Transform closedChatParent;

    [Header("UI References")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TextMeshProUGUI currentChatOption;

    [Header("Chat Settings")]
    [SerializeField] private float maxTimeBeforeClosedChatClosesAfterNoMessages = 15;
    private float timeSinceLastMessage = 0;
    private bool isChattingGlobally;

    [Header("Spam Detection")]
    [SerializeField] private int maxAmountOfText = 5; // max amount of time 
    [SerializeField] private float spamDuration; // time before user can type again
    public bool canText = true;
    private int amountOfTextSent = 0;
    private bool isMuted = false;

    private void Start()
    {
        if (!IsOwner)
        {
            chatBoxObj.SetActive(false);
            return;
        }

        instance = this;

        chatBoxObj.SetActive(true);
        closedChat.SetActive(true);
        openedChat.SetActive(false);

        isChattingGlobally = true;
        currentChatOption.text = "(ALL)";
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        HandleChatToggleInput();

        // switch from ALL -> Team or Team -> All
        if (openedChat.activeInHierarchy)
            HandleChatChannelSwitch();

        if (closedChat.activeInHierarchy)
        {
            timeSinceLastMessage += Time.deltaTime;

            // if there was no text messages we can close the "closed" chat object
            if (timeSinceLastMessage > maxTimeBeforeClosedChatClosesAfterNoMessages)
                closedChat.SetActive(false);
        }

        // handle spamming detection and prevent them from doing it
        HandleSpamming();
    }

    private void HandleChatToggleInput()
    {
        if (enableChatAction.action.WasPressedThisFrame() && !quickChatObj.activeInHierarchy && !emoteChatObj.activeInHierarchy)
        {
            // open up the chatbox
            if (!openedChat.activeInHierarchy && !HandleCursorSettings.instance.IsUIOn())
            {
                openedChat.SetActive(true);
                closedChat.SetActive(false);

                // allow cursor movement
                HandleCursorSettings.instance.EnableCursor(true, false);
                PlayerMovement.instance.DisableMovement(true);

                // autofocuses the inputfield
                inputField.Select();
                inputField.ActivateInputField();
            }

            // or close it
            else
            {
                // disable cursor movement
                HandleCursorSettings.instance.EnableCursor(false, true);
                PlayerMovement.instance.DisableMovement(false);

                timeSinceLastMessage = 0;
                closedChat.SetActive(true);
                openedChat.SetActive(false);
            }
        }
    }

    private void HandleChatChannelSwitch()
    {
        // change the chat option
        if (changeChatOptionAction.action.WasPressedThisFrame())
        {
            isChattingGlobally = !isChattingGlobally;
            currentChatOption.text = isChattingGlobally ? "(ALL)" : "(TEAM)";
        }
    }

    private void HandleSpamming()
    {
        if (amountOfTextSent >= maxAmountOfText && !isMuted)
            StartCoroutine(PreventSendingMessage());
    }

    public void HandleTrackingAmountOfTextSent()
    {
        amountOfTextSent++;
        Invoke(nameof(DecreaseAmountOfTextSentInt), 2f);
    }

    private IEnumerator PreventSendingMessage()
    {
        isMuted = true;
        canText = false;
        inputField.interactable = false;

        float startTime = Time.time;
        while (Time.time - startTime <= spamDuration)
        {
            float remainingTime = spamDuration - (Time.time - startTime);
            inputField.text = $"You must wait {Mathf.CeilToInt(remainingTime)} seconds before texting again!";
            yield return null;
        }

        canText = true;
        inputField.interactable = true;
        inputField.text = "";
        amountOfTextSent = 0;
        isMuted = false;
    }

    private void DecreaseAmountOfTextSentInt()
    {
        // this function is called every time a user sends a message
        // essentially, if the user spams a message and reaches the max amount of texts allocated
        // they will be muted
        // but if they dont spam, this allows the user to freely text without any mute
        if (amountOfTextSent > 0)
            amountOfTextSent--;
    }

    public void HandleFormattingTexts(bool isChattingAll, int rankIndex, string teamColor, string username, string position, string text, bool isServer = false)
    {
        // if there isnt a message
        if (string.IsNullOrEmpty(text))
            return;

        GameObject openTextObj = Instantiate(openChatText, openChatParent);
        GameObject closedTextObj = Instantiate(closedChatText, closedChatParent);

        string formattedText;

        // if the server sent the message
        if (isServer)
            formattedText = $"<color=yellow>{text}";

        else
        {
            string chatOption = isChattingAll ? "(ALL)" : "(TEAM)";
            string nameColor;

            // if we sent out the message
            if (username == HandlePlayerData.instance.GetUsername())
                nameColor = "yellow";

            // if on the same team, make the username text blue
            // #26B5E3 == light blue
            else if (teamColor == PlayerInfo.instance.currentTeam.Value)
                nameColor = "#26B5E3";

            // if not on the same team
            else
                nameColor = "red";

            // add the spaces to give room for the rank image
            formattedText = $"<color=white>{chatOption}       <color={nameColor}>{username} <color=white>({position}):<color=white> {text}";
        }

        openTextObj.GetComponent<HandleChatBoxTextPrefabs>().SetChatBoxText(rankIndex, formattedText, isChattingAll);
        closedTextObj.GetComponent<HandleChatBoxTextPrefabs>().SetChatBoxText(rankIndex, formattedText, isChattingAll);

        // reset the time since last message
        timeSinceLastMessage = 0;

        // and open the closed chat agian if it was inactive
        if (!openedChat.activeInHierarchy && !closedChat.activeInHierarchy)
            closedChat.SetActive(true);
    }
    public void SendMessage()
    {
        // if still in cooldown or nothing is typed
        if (!canText || string.IsNullOrWhiteSpace(inputField.text))
            return;

        HandleTrackingAmountOfTextSent();

        SendTextServerRpc(isChattingGlobally, PlayerInfo.instance.rankIndex.Value, PlayerInfo.instance.currentTeam.Value.ToString(), HandlePlayerData.instance.GetUsername(), PlayerInfo.instance.currentPosition.Value.ToString(), inputField.text);

        // reset the inputfield text
        inputField.text = "";
        inputField.ActivateInputField();
    }

    [ServerRpc]
    public void SendTextServerRpc(bool isChattingAll, int rankIndex, string teamColor, string username, string position, string text, ServerRpcParams serverRpcParams = default)
    {
        // send to everyone
        if (isChattingAll)
            SendTextClientRpc(isChattingAll, rankIndex, teamColor, username, position, text);

        // send to team only
        else
        {
            var targetClientIds = teamColor.Equals("Blue") ? ServerManager.instance.blueTeamPlayerIds.ToArray() : ServerManager.instance.redTeamPlayerIds.ToArray();

            var rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = targetClientIds
                }
            };

            SendTextClientRpc(isChattingAll, rankIndex, teamColor, username, position, text, false, rpcParams);
        }
    }

    [ClientRpc]
    public void SendTextClientRpc(bool isChattingAll, int rankIndex, string teamColor, string username, string position, string text, bool isServer = false, ClientRpcParams clientRpcParams = default)
    {
        HandleChatbox chatbox = instance ?? NetworkManager.LocalClient.PlayerObject.GetComponentInChildren<HandleChatbox>();

        chatbox.HandleFormattingTexts(isChattingAll, rankIndex, teamColor, username, position, text, isServer);
    }
}