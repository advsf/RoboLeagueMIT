using UnityEngine;
using Unity.Netcode;

public class EscapeMenuHandler : NetworkBehaviour
{
    private void Update()
    {
        if (!IsOwner)
            return;

        if (PlayerMovement.instance != null)
            if (PlayerMovement.instance.isSliding || PlayerMovement.instance.isMovementDisabled)
                return;

        // just activiate the ui
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // if we need to disable the canva obj
            if (ServerManager.instance.IsSpawnSelectionCanvaObjActive())
            {
                HandleCursorSettings.instance.EnableCursor(false, true);

                ServerManager.instance.EnableSpawnSelectionCanvaObj(false);
            }

            else
            {
                HandleCursorSettings.instance.EnableCursor(true, false);

                ServerManager.instance.EnableSpawnSelectionCanvaObj(true);
            }
        }
    }
}