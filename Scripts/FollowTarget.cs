using UnityEngine;
using Unity.Netcode;
public class FollowTarget : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform targetPos;
    [SerializeField] private Transform targetRot;

    [Header("Settings")]
    [SerializeField] private float originalXRot;
    [SerializeField] private float originalYRot;
    [SerializeField] private float originalZRot;
    [SerializeField] private float positionLerpSpeed = 1f;
    [SerializeField] private float rotationLerpSpeed = 1f;

    [Header("Checks")]
    [SerializeField] private bool followPos;
    [SerializeField] private bool followXRot;
    [SerializeField] private bool followYRot;
    [SerializeField] private bool followZRot;
    [SerializeField] private bool applyPosLerp;
    [SerializeField] private bool applyRotLerp;

    private void Update()
    {
        if (!IsOwner)
            return;

        if (followPos)
        {
            // lerp the change in position
            if (applyPosLerp)
                transform.position = Vector3.Lerp(transform.position, targetPos.position, Time.deltaTime * positionLerpSpeed);

            else
                transform.position = targetPos.position;
        }

        // get the calculated angle
        Vector3 targetEuler = new Vector3(
            followXRot ? targetRot.rotation.eulerAngles.x : originalXRot,
            followYRot ? targetRot.rotation.eulerAngles.y : originalYRot,
            followZRot ? targetRot.rotation.eulerAngles.z : originalZRot
        );

        // apply changes with a lerp
        if (applyRotLerp)
        {
            Quaternion targetRotation = Quaternion.Euler(targetEuler);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationLerpSpeed);
        }

        else
            transform.rotation = Quaternion.Euler(targetEuler);
    }
}
