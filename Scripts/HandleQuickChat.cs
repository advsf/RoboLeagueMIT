using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class HandleQuickChat : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject quickChatObj;
    [SerializeField] private GameObject chatBoxObj;
    [SerializeField] private GameObject emoteChatObj;
    [SerializeField] private HandleChatbox chatMessageScript;
    public InputActionReference openQuickChatAction;

    private int amountOfQuickChatTextsSent;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        quickChatObj.SetActive(false);

        base.OnNetworkSpawn();
    }

    private void Update()
    {
        if (!IsOwner)
            return;

       if (openQuickChatAction.action.WasCompletedThisFrame() && !chatBoxObj.activeInHierarchy && !emoteChatObj.activeInHierarchy)
            quickChatObj.SetActive(!quickChatObj.activeInHierarchy);

        // handle which messages to send
        if (!quickChatObj.activeInHierarchy)
            return;

        // 1 - GG! (global)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SendMessageOnChat("GG!", true);
        }

        // 2 - Well played! (global)
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SendMessageOnChat("Well played!", true);
        }

        // 3 - Good save! (global)
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SendMessageOnChat("Good save!", true);
        }

        // 4 - Wow! (global)
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SendMessageOnChat("Wow!", true);
        }

        // 5 - Pass! (team)
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            SendMessageOnChat("Pass!", false);
        }

        // 6 - Cross! (team)
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            SendMessageOnChat("Cross!", false);
        }

        // 7 - Make a run! (team)
        else if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            SendMessageOnChat("Make a run!", false);
        }

        // 8 - Defend! (team)
        else if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            SendMessageOnChat("Defend!", false);
        }
    }

    private void SendMessageOnChat(string text, bool isChattingGlobally)
    {
        if (!chatMessageScript.canText)
            return;

        // server logic
        if (IsServer)
        {
            // if sending to everyone
            if (isChattingGlobally)
            {
                chatMessageScript.SendTextClientRpc(isChattingGlobally, PlayerInfo.instance.rankIndex.Value, PlayerInfo.instance.currentTeam.Value.ToString(), PlayerInfo.instance.username.Value.ToString(), PlayerInfo.instance.currentPosition.Value.ToString(), text);
            }

            // if sending only to team
            else
            {
                var targetClientIds = PlayerInfo.instance.currentTeam.Value.Equals("Blue") ? ServerManager.instance.blueTeamPlayerIds.ToArray() : ServerManager.instance.redTeamPlayerIds.ToArray();

                var rpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = targetClientIds
                    }
                };


                chatMessageScript.SendTextClientRpc(isChattingGlobally, PlayerInfo.instance.rankIndex.Value, PlayerInfo.instance.currentTeam.Value.ToString(), PlayerInfo.instance.username.Value.ToString(), PlayerInfo.instance.currentPosition.Value.ToString(), text, false, rpcParams);
            }
        }

        // client logic
        else
            chatMessageScript.SendTextServerRpc(isChattingGlobally, PlayerInfo.instance.rankIndex.Value, PlayerInfo.instance.currentTeam.Value.ToString(), HandlePlayerData.instance.GetUsername(), PlayerInfo.instance.currentPosition.Value.ToString(), text);

        // spam detection
        chatMessageScript.HandleTrackingAmountOfTextSent();

        // close the quickchat
        quickChatObj.SetActive(false);
    }
}
