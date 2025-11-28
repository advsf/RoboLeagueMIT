using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.EventSystems;

public class DisplayJoinCode : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI codeText;
    [SerializeField] private Button copyCodeButton;
    [SerializeField] private SessionHolder sessionHolder;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {            
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        DisplayCode();
    }

    private void DisplayCode()
    {
        codeText.text = sessionHolder.ActiveSession?.Code ?? "";
        copyCodeButton.interactable = true;
    }

    public void CopySessionCodeToClipboard()
    {
        // deselect the button when clicked
        EventSystem.current.SetSelectedGameObject(null);

        string code = codeText.text;

        // if there is no code (for some reason)
        if (sessionHolder.ActiveSession?.Code == null || string.IsNullOrEmpty(code))
            return;

        // copy the text to the clipboard
        GUIUtility.systemCopyBuffer = code;
    }
}
