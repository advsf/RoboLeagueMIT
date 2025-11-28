using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class HandleEmotes : NetworkBehaviour
{
    [Header("Input References")]
    public InputActionReference emoteKey;

    [Header("References")]
    [SerializeField] private GameObject emoteUI;
    [SerializeField] private GameObject quickChatUI;
    [SerializeField] private GameObject chatBoxUI;
    [SerializeField] private AnimationStateController playerAnimationController;

    [Header("Animation Duration Settings")]
    [SerializeField] private float emote1Duration = 7;
    [SerializeField] private float emote2Duration;
    [SerializeField] private float emote3Duration = 1.4f;
    [SerializeField] private float emote4Duration = 1.4f;

    // only reason we dont use emoteUI.activeInHierarchy is because when we disable the HUD, we can't exactly use that
    // this allows the user to still play emotes even when the user disabled HUD
    private bool isEnabled = false; 
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        emoteUI.SetActive(false);

        base.OnNetworkSpawn();
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (emoteKey.action.WasCompletedThisFrame() && !quickChatUI.activeInHierarchy && !chatBoxUI.activeInHierarchy)
        {
            emoteUI.SetActive(!emoteUI.activeInHierarchy);
            isEnabled = emoteUI.activeInHierarchy;
        }

        // if the emote UI isn't open
        if (!isEnabled)
            return;

        // 1 - Pray
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            playerAnimationController.PlayEmote(1, emote1Duration);

            isEnabled = false;
            emoteUI.SetActive(isEnabled);
        }

        // 2 - MY LOVE FOR YOU
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            playerAnimationController.PlayEmote(2, emote2Duration);

            isEnabled = false;
            emoteUI.SetActive(isEnabled);
        }

        // 3 - Backflip
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            playerAnimationController.PlayEmote(3, emote3Duration); // low cooldown to make it only play once

            isEnabled = false;
            emoteUI.SetActive(isEnabled);
        }

        // 4 - Take the L
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            playerAnimationController.PlayEmote(4, emote4Duration); // low cooldown tot make it only play once

            isEnabled = false;
            emoteUI.SetActive(isEnabled);
        }
    }
}
