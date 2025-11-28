using UnityEngine;
using Unity.Netcode;

public class HandleNonClientSettings : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CapsuleCollider mainCollider;
    [SerializeField] private GameObject[] objs;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // disable the things that non owners shouldnt see
        if (!IsOwner)
        {
            mainCollider.enabled = false;

            foreach (GameObject obj in objs)
                obj.SetActive(false);
        }
    }
}
