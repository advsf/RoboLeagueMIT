using UnityEngine;
using Unity.Netcode;
public class HandleNetworkPlayerSettings : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject[] gameObjToDestroy;
    public override void OnNetworkSpawn()
    {
        // destroy the things that only the owner should have
        if (!IsOwner)
            foreach (GameObject obj in gameObjToDestroy)
                Destroy(obj);

        base.OnNetworkSpawn();
    }
}
