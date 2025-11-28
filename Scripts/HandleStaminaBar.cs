using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class HandleStaminaBar : NetworkBehaviour
{
    [Header("Setting")]
    [SerializeField] private float pulseSpeed;
    [SerializeField] private Color rechargeColor;
    private Color regularColor;

    [Header("References")]
    [SerializeField] private Image staminaBar;
    [SerializeField] private float smoothFactor;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            gameObject.SetActive(false);
            return;
        }

        PlayerMovement.onStaminaChanged += HandleChangingStaminaBar;

        regularColor = staminaBar.color;

        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
            PlayerMovement.onStaminaChanged -= HandleChangingStaminaBar;

        base.OnNetworkDespawn();
    }

    private void HandleChangingStaminaBar(float currentStamina)
    {
        if (!IsOwner)
            return;

        staminaBar.fillAmount = Mathf.MoveTowards(staminaBar.fillAmount, (currentStamina / PlayerMovement.instance.maxStamina) * 0.5f, Time.deltaTime * smoothFactor);

        // if we are recharging after the user depleted his stamina
        if (PlayerMovement.instance.isStaminaRecharging)
            HandleStaminaRechargeColor();
        else
            staminaBar.color = Color.Lerp(staminaBar.color, regularColor, Time.deltaTime * smoothFactor);
    }

    private void HandleStaminaRechargeColor()
    {
        float alpha = (Mathf.Sin(Time.time * pulseSpeed)) / 2f;

        // apply the color with the calculated alpha
        staminaBar.color = new Color(rechargeColor.r, rechargeColor.g, rechargeColor.b, alpha);
    }
}
