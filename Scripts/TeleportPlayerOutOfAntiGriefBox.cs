using UnityEngine;
using Unity.Netcode;

public class TeleportPlayerOutOfAntiGriefBox : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform teleportPlace;
    [SerializeField] private BallSync ballSync;

    [Header("Settings")]
    [SerializeField] private bool isThrowIn = true;

    private void OnCollisionStay(Collision collision)
    {
        // if not kinematics
        if (!ballSync.GetRigidbody().isKinematic && isThrowIn)
            return;

        Transform hitObj = collision.collider.transform.root;
        Debug.Log(hitObj.name);

        if (hitObj.GetComponent<PlayerInfo>())
        {
            PlayerInfo playerInfo = hitObj.GetComponent<PlayerInfo>();

            // if we are the player (and also not holding the ball)
            if (hitObj.GetComponent<NetworkObject>().OwnerClientId == NetworkManager.LocalClientId && playerInfo.playingObj.activeInHierarchy && !playerInfo.GetComponentInChildren<HandleThrowIn>().isPickedUp)
                PlayerMovement.instance.TeleportPlayerToPosition(teleportPlace.position);
        }
    }
}
