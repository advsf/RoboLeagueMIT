using UnityEngine;
using UnityEngine.InputSystem;

public class SpinLobbyPlayer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float spinSensitivity;

    private float mouseX;

    private void Update()
    {
        mouseX = Mouse.current.delta.ReadValue().x;

        if (mouseX != 0 && Input.GetMouseButton(0))
            transform.Rotate(Vector3.up, -mouseX * spinSensitivity, Space.World);
    }
}
