using UnityEngine;
using Unity.Netcode;

public class HandleInnerDotScale : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Transform innerDot;
    [SerializeField] private Transform soccerBall;

    [Header("Setting")]
    [SerializeField] private float maxDistance = 20f;
    [SerializeField] private float scaleLerpFactor;

    private void Update()
    {
        if (PlayerInfo.instance == null)
            return;

        if (FBPP.GetInt("EnableBallDot") != 1)
        {
            innerDot.gameObject.SetActive(false);
            return;
        }

        else
            innerDot.gameObject.SetActive(true);

        float minScale = FBPP.GetFloat("BallDotMinSize");
        float maxScale = FBPP.GetFloat("BallDotMaxSize");

        float distanceFromBall = Vector3.Distance(soccerBall.position, NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerInfo>().GetCurrentActiveObjectTransform().position);

        float targetScale = Mathf.Lerp(minScale, maxScale, distanceFromBall / maxDistance);

        float newScale = Mathf.Lerp(innerDot.localScale.x, targetScale, Time.deltaTime * scaleLerpFactor);

        innerDot.localScale = new Vector3(newScale, newScale, newScale);
    }
}
