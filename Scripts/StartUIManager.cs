using UnityEngine;

public class StartUIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject startUIObj;

    public void OpenSettingsMenu()
    {
        HandleSettings.instance.OpenSettingsUI();
    }

    public void ChangeSpectatorMode()
    {
        startUIObj.SetActive(false);

        PlayerInfo.instance.ChangeIntoSpectatorMode();
    }

}
