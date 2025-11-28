using UnityEngine;
using TMPro;
using Unity.Services.Multiplayer;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class SessionItemData : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [SerializeField] private TextMeshProUGUI sessionNameText;
    [SerializeField] private TextMeshProUGUI sessionPlayersCount;
    [SerializeField] private TextMeshProUGUI sessionRegionText;

    public UnityEvent<ISessionInfo> OnSessionSelected;
    public UnityEvent OnSessionDeselected;

    private ISessionInfo sessionInfo;

    public void SetSession(ISessionInfo sessionInfo)
    {
        this.sessionInfo = sessionInfo;
        SetSessionNameText(sessionInfo.Name);
        SetSessionPlayerCountText(sessionInfo.MaxPlayers - sessionInfo.AvailableSlots, sessionInfo.MaxPlayers);
        SetSessionRegionText();
    }

    public void SetSessionNameText(string sessionName) => sessionNameText.text = sessionName;

    public void SetSessionPlayerCountText(int currentPlayers, int maxPlayers) => sessionPlayersCount.text = $"{currentPlayers}/{maxPlayers}";

    public void SetSessionRegionText() => sessionRegionText.text = sessionInfo.Properties["Region"].Value;

    public void OnSelect(BaseEventData eventData)
    {
        OnSessionSelected?.Invoke(sessionInfo);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        OnSessionDeselected?.Invoke();
    }
}
