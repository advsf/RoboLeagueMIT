using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class ChangeShootingSilhouette : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Sprite dribblingSilhouette;
    [SerializeField] private Sprite shootingSilhouette;
    [SerializeField] private GameObject silhouetteForeground;
    [SerializeField] private GameObject silhouetteShadow;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            silhouetteForeground.SetActive(false);
            silhouetteShadow.SetActive(false);
        }

        base.OnNetworkSpawn();
    }

    public void ChangeSilhouette(bool isDribbling)
    {
        if (!IsOwner)
            return;

        if (isDribbling)
        {
            silhouetteForeground.GetComponent<Image>().sprite = dribblingSilhouette;
            silhouetteShadow.GetComponent<Image>().sprite = dribblingSilhouette;
        }

        else
        {
            silhouetteForeground.GetComponent<Image>().sprite = shootingSilhouette;
            silhouetteShadow.GetComponent<Image>().sprite = shootingSilhouette;
        }
    }
}
