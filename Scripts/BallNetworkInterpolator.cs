using UnityEngine;

public class VisualInterpolator : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float interpolationSpeed = 15f;

    void Update()
    {
        if (target == null) 
            return;

        transform.position = Vector3.Lerp(transform.position, target.position, Time.deltaTime * interpolationSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, Time.deltaTime * interpolationSpeed);
    }
}