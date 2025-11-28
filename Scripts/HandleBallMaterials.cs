using UnityEngine;

public class HandleBallMaterials : MonoBehaviour
{
    [Header("Mat References")]
    [SerializeField] private Material[] matWithoutOutline;
    [SerializeField] private Material[] matWithOutline; // this mat array has the outline 
    [SerializeField] private Renderer renderer;
    [SerializeField] private Rigidbody ballRb;

    [Header("Toon Shader Settings")]
    [SerializeField] private float ballVelocityThreshold; // how high the ball should be

    private void Start()
    {
        // only have the soccerMat active in the beginning
        renderer.materials = matWithoutOutline;
    }

    private void Update()
    {
        // if the ball is high or moving fast
        if (ballRb.linearVelocity.magnitude >= ballVelocityThreshold && renderer.materials.Length < 2)
            renderer.materials = matWithOutline;

        // if it isn't
        // remove the outline (toon shader)
        if (ballRb.linearVelocity.magnitude < ballVelocityThreshold && renderer.materials.Length == 2)
            renderer.materials = matWithoutOutline;

    }
}
