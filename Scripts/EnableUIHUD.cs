using UnityEngine;

// we can do mono because we destory this behaviour for non owners
public class EnableUIHUD : MonoBehaviour
{
    [SerializeField] private GameObject uiHolder;

    private void Start()
    {
        ToggleHUD(FBPP.GetInt("EnableHud") == 1);

        // subscribe to the event where we change the value of toggle HUD
        HandleSettings.OnHUDToggled += ToggleHUD;
    }

    private void OnDestroy()
    {
        HandleSettings.OnHUDToggled -= ToggleHUD;
    }

    private void ToggleHUD(bool enable)
    {
        Debug.Log(enable);

        uiHolder.SetActive(enable);
    }
}
