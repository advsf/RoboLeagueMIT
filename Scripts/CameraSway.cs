using UnityEngine;

public class CameraSway : MonoBehaviour
{
    [Header("Sway Settings")]
    public float swayAmount = 0.2f;          
    public float swaySpeed = 0.5f;         
    public float rotationAmount = 1.0f;      
    public float rotationSpeed = 0.3f;     

    private Vector3 startPos;
    private Quaternion startRot;

    private void Start()
    {
        startPos = transform.position;
        startRot = transform.rotation;
    }

    private void Update()
    {
        float time = Time.time;

        // subtle position movement
        float swayX = Mathf.Sin(time * swaySpeed) * swayAmount;
        float swayY = Mathf.Cos(time * (swaySpeed * 0.7f)) * swayAmount * 0.5f;
        float swayZ = Mathf.Sin(time * (swaySpeed * 0.5f)) * swayAmount * 0.3f;

        transform.position = startPos + new Vector3(swayX, swayY, swayZ);

        // gentle rotation sway
        float rotX = Mathf.Sin(time * rotationSpeed) * rotationAmount;
        float rotY = Mathf.Cos(time * rotationSpeed * 0.8f) * rotationAmount * 0.5f;
        transform.rotation = startRot * Quaternion.Euler(rotX, rotY, 0f);
    }
}
