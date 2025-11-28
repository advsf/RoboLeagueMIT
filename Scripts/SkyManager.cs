using UnityEngine;

public class SkyManager : MonoBehaviour
{
    [SerializeField] private float skySpeed;

    private void Update()
    {
        // rotates the skybox
        RenderSettings.skybox.SetFloat("_Rotation", Time.time * skySpeed);
    }
}
