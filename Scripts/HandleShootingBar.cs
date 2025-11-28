using UnityEngine;
using Unity.Netcode;

public class HandleShootingBar : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject shootingBar;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            shootingBar.SetActive(false);

        base.OnNetworkSpawn();
    }
}
