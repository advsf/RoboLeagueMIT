using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class HandleChatBoxTextPrefabs : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image rankImage;
    [SerializeField] private Image rankImageShadow;
    [SerializeField] private TextMeshProUGUI text;

    [Header("Rank Image Position Setting")]
    [SerializeField] private float allChatPos;
    [SerializeField] private float teamChatPos;

    public void SetChatBoxText(int rankIndex, string chatText, bool isChattingAll)
    {
        if (rankIndex >= 0)
        {
            rankImage.sprite = HandlePlayerData.instance.GetRankSprite(rankIndex);
            rankImageShadow.sprite = HandlePlayerData.instance.GetRankSprite(rankIndex);
        }

        else
        {
            rankImage.enabled = false;
            rankImageShadow.enabled = false;
        }

        // set the position of the rank imgs
        RectTransform rankImgRect = rankImage.rectTransform;
        RectTransform rankShadowImgRect = rankImageShadow.rectTransform;

        rankImgRect.anchoredPosition = new(isChattingAll ? allChatPos : teamChatPos, rankImgRect.anchoredPosition.y);
        rankShadowImgRect.anchoredPosition = new(isChattingAll ? allChatPos : teamChatPos, rankShadowImgRect.anchoredPosition.y);

        text.text = chatText;
    }
}
