using UnityEngine;
using TMPro;
using Unity.Netcode;

public class HandlePlayerNameTag : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI playerTag;
    [SerializeField] private PlayerInfo playerInfo;

    private Transform localPlayerCam;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            if (NetworkManager.LocalClient.PlayerObject.GetComponentInChildren<Camera>() != null)
                AssignCamera();

            playerTag.text = playerInfo.username.Value.ToString();
        }

        // disable the nametag for the owner
        else
            gameObject.SetActive(false);

        base.OnNetworkSpawn();
    }

    private void Update()
    {
        if (!IsOwner && IsSpawned)
        {
            if (localPlayerCam == null)
                AssignCamera();

            if (string.IsNullOrEmpty(playerTag.text))
                playerTag.text = playerInfo.username.Value.ToString();

            if (localPlayerCam != null)
            {
                if (localPlayerCam.gameObject != null)
                    transform.rotation = localPlayerCam.rotation;
            }
        }
    }

    private void AssignCamera()
    {
        if (NetworkManager.Singleton == null || NetworkManager.LocalClient == null || NetworkManager.LocalClient.PlayerObject == null)
            return;

        Camera cam = NetworkManager.LocalClient.PlayerObject.GetComponentInChildren<Camera>();
        if (cam != null)
            localPlayerCam = cam.transform;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        localPlayerCam = null; // Clear the reference to prevent accessing a destroyed object
    }
}
