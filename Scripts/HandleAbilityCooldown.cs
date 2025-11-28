using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;

// just so you know
// this handles the MOVEMENT abilities
// not the power abilities
public class HandleAbilityCooldown : NetworkBehaviour
{
    [Header("Setting")]
    [SerializeField] private float smoothAmount;
    [SerializeField] private float maxForegroundAlpha = 1;
    [SerializeField] private float maxBackgroundAlpha = 0.5058824f;

    [Header("References")]
    [SerializeField] private Image[] images; // should be two pairs of images

    private Image[,] UIImages; // row contains 2 column (background and foreground)

    private int abilityIndex; // used to determine which UI to use (slide or dash)
    private float cooldown;

    private void OnEnable()
    {
        if (!IsOwner)
        {
            foreach (Image img in images)
                img.gameObject.SetActive(false);

            return;
        }

        foreach (Image img in images)
            img.gameObject.SetActive(true);

        int cumIndex = 0; // type shit

        UIImages = new Image[images.Length / 2, 2];
        // add images into the 2d array
        // effectively pairing them into their own row
        for (int i = 0; i < images.Length / 2; i++)
            for (int j = 0; j < 2; j++)
                UIImages[i, j] = images[cumIndex++];

        base.OnNetworkSpawn();
    }

    public void StartCooldown(string name, float cooldown)
    {
        this.cooldown = cooldown;

        // determine which UI to handle
        switch (name)
        {
            case "Slide":
                abilityIndex = 0;
                break;
            case "Dash":
                abilityIndex = 1;
                break;
            default:
                return;
        }

        StartCoroutine(HandleUICooldown());
    }

    private IEnumerator HandleUICooldown()
    {
        float time = Time.time;

        Image foreground = UIImages[abilityIndex, 0];
        Image background = UIImages[abilityIndex, 1];

        foreground.fillAmount = 0;
        background.fillAmount = 0;

        // while we're still in cooldown
        while (Time.time - time <= cooldown)
        {
            float elapsedTime = Time.time - time;

            // handle the fill amount
            float calculatedFillAmount = elapsedTime / cooldown;

            foreground.fillAmount = calculatedFillAmount;
            background.fillAmount = calculatedFillAmount;

            // handle the alpha for foreground
            float calculatedForegroundAlpha = elapsedTime / cooldown * maxForegroundAlpha;
            float calculatedBackgroundAlpha = elapsedTime / cooldown * maxBackgroundAlpha;

            foreground.color = new(foreground.color.r, foreground.color.g, foreground.color.b, calculatedForegroundAlpha);
            background.color = new(background.color.r, background.color.g, background.color.b, calculatedBackgroundAlpha);

            yield return null;
        }

        // ensure that the ending results are normal
        foreground.fillAmount = 1;
        background.fillAmount = 1;

        foreground.color = new(foreground.color.r, foreground.color.g, foreground.color.b, maxForegroundAlpha);
        background.color = new(background.color.r, background.color.g, background.color.b, maxBackgroundAlpha);
    }
}
