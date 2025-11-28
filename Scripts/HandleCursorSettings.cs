using UnityEngine;

public class HandleCursorSettings : MonoBehaviour
{
    public static HandleCursorSettings instance;

    private bool isUIOn;

    private void Start()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        else
        {
            Destroy(gameObject);
        }

        EnableCursor(true);
    }

    public void EnableCursor(bool condition, bool canCamMove = true)
    {
        // if we should enable the cursor
        if (condition)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            isUIOn = true;
        }

        // disable cursor
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            isUIOn = false;
        }

        CameraLook.instance.EnableCamera(canCamMove);
    }

    public void SetUIOnMode(bool condition) => isUIOn = condition;

    public bool IsUIOn() => isUIOn;
}
