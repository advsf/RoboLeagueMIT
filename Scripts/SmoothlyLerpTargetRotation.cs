using UnityEngine;

public class SmoothlyLerpTargetRotation : MonoBehaviour
{
    [Header("Setting")]
    [SerializeField] private bool lerpXRot;
    [SerializeField] private bool lerpYRot;
    [SerializeField] private bool lerpZRot;
    

    [Header("References")]
    [SerializeField] private Transform targetRot;
    [SerializeField] private float lerpSpeed;

    private void Update()
    {
        // transform.rotation = Quaternion.Lerp(transform.rotation, targetQuaternionRot, Time.deltaTime * lerpSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, 
            new(
            lerpXRot ? targetRot.rotation.x : transform.rotation.x, 
            lerpYRot ? targetRot.rotation.y : transform.rotation.y, 
            lerpZRot ? targetRot.rotation.z : transform.rotation.z, 
            targetRot.rotation.w), 
            Time.deltaTime * lerpSpeed);
    }
}
