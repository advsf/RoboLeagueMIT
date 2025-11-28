using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HandleSessionSettingsUI : MonoBehaviour
{
    [Header("Session Name References")]
    [SerializeField] private TMP_InputField sessionNameField;
    [SerializeField] private Button sessionCreateButton;
    [SerializeField] private int minimumCharLength;
    [SerializeField] private int maximumCharLength;

    [Header("Private Setting Toggle References")]
    [SerializeField] private Toggle privateCheckToggle;
    [SerializeField] private Image lockPadletIcon;
    [SerializeField] private Image unlockedPadletIcon;

    [Header("Join Session By Code Settings")]
    [SerializeField] private TMP_InputField sessionIdField;
    [SerializeField] private Button sessionIdJoinButton;
    [SerializeField] private int mimimumIdLength;
    [SerializeField] private int maximumIdLength;

    private void Start()
    {
        // default settings
        sessionCreateButton.interactable = false;
        sessionIdJoinButton.interactable = false;

        privateCheckToggle.isOn = false;
        unlockedPadletIcon.enabled = false;
        unlockedPadletIcon.enabled = true;
    }

    // called by exposed events
    public void CheckIfSessionNameIsValid()
    {
        // if invalid name length
        if (sessionNameField.text.Length < minimumCharLength || sessionNameField.text.Length > maximumCharLength)
            sessionCreateButton.interactable = false;

        else
            sessionCreateButton.interactable = true;
    }

    public void HandleToggleIcon()
    {
        // if our current privacy setting is set to locked
        // change it back to unlock
        // this script only handles the visual aspect
        if (privateCheckToggle.isOn)
        {
            lockPadletIcon.enabled = true;
            unlockedPadletIcon.enabled = false;
        }

        else
        {
            lockPadletIcon.enabled = false;
            unlockedPadletIcon.enabled = true;
        }
    }

    public void CheckifSessionIdIsValid()
    {
        if (sessionIdField.text.Length < mimimumIdLength || sessionIdField.text.Length > maximumIdLength)
            sessionIdJoinButton.interactable = false;

        else
            sessionIdJoinButton.interactable = true;
    }
}
