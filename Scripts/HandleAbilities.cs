using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HandleAbilities : NetworkBehaviour
{
    public static HandleAbilities instance;

    [Header("Ability Slots")]
    [SerializeField] private Abilities leftAbility;
    [SerializeField] private Abilities rightAbility;

    [Header("Left Ability UI References")]
    [SerializeField] private GameObject leftAbilityObj;
    [SerializeField] private Image leftAbilityBackground;
    [SerializeField] private TextMeshProUGUI leftAbilityName;
    [SerializeField] private TextMeshProUGUI leftAbilityKeyForeground;
    [SerializeField] private TextMeshProUGUI leftAbilityKeyBackground;

    [Header("Right Ability UI References")]
    [SerializeField] private GameObject rightAbilityObj;
    [SerializeField] private Image rightAbilityBackground;
    [SerializeField] private TextMeshProUGUI rightAbilityName;
    [SerializeField] private TextMeshProUGUI rightAbilityKeyForeground;
    [SerializeField] private TextMeshProUGUI rightAbilityKeyBackground;

    [Header("Animation References")]
    [SerializeField] private Animator leftAnimator;
    [SerializeField] private Animator rightAnimator;

    [Header("Ability Slots")]
    [SerializeField] private Abilities powerKickAbility;
    [SerializeField] private Abilities speedsterAbility;
    [SerializeField] private Abilities trapAbility;
    [SerializeField] private Abilities rouletteAbility;
    [SerializeField] private Abilities deflectAbility;
    [SerializeField] private Abilities killAbility;

    [Header("Other References")]
    [SerializeField] private float maxBackgroundAlpha;
    [SerializeField] private HandleThrowIn throwInScript;
    public InputActionReference leftAbilityAction;
    public InputActionReference rightAbilityAction;

    private float leftCooldownTimer;
    private float rightCooldownTimer;

    private float leftCurrentCooldownDuration;
    private float rightCurrentCooldownDuration;

    private bool isLeftAbilityTriggered;
    private bool isRightAbilityTriggered;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            leftAbilityObj.SetActive(false);
            rightAbilityObj.SetActive(false);
            return;
        }

        instance = this;

        isLeftAbilityTriggered = false;
        isRightAbilityTriggered = false;

        leftAbilityAction.action.Enable();
        rightAbilityAction.action.Enable();
    }

    public void SetPositionAbilities(string position)
    {
        if (position.Equals("CF"))
        {
            leftAbility = powerKickAbility;
            rightAbility = speedsterAbility;
        }

        else if (position.Equals("RMF") || position.Equals("LMF"))
        {
            leftAbility = trapAbility;
            rightAbility = rouletteAbility;
        }

        else if (position.Equals("CB"))
        {
            leftAbility = killAbility;
            rightAbility = deflectAbility;
        }

        else
        {
            leftAbility = null;
            rightAbility = null;
        }

        // set up the animator component to the UI animators (which allows the rainbow display thing when activiated)
        if (leftAbility != null)
            leftAbility.animator = leftAnimator;
        
        if (rightAbility != null)
            rightAbility.animator = rightAnimator;

        HandleUI();
    }

    private void OnEnable()
    {
        if (!IsOwner)
            return;

        leftAbilityAction.action.Enable();
        rightAbilityAction.action.Enable();
    }

    private void OnDisable()
    {
        if (!IsOwner)
            return;

        leftAbilityAction.action.Disable();
        rightAbilityAction.action.Disable();
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        HandleCooldown();

        if (PlayerMovement.instance.isMovementDisabled || HandleCursorSettings.instance.IsUIOn() || throwInScript.isPickedUp)
            return;

        if (leftAbilityAction.action.IsPressed() && leftAbility != null)
            PerformLeftAbility();

        if (rightAbilityAction.action.IsPressed() && rightAbility != null)
            PerformRightAbility();
    }

    private void HandleUI()
    {
        if (rightAbility != null)
            rightAbilityName.text = rightAbility.abilityName;
        else
            rightAbilityName.text = "None";

        if (leftAbility != null)
            leftAbilityName.text = leftAbility.abilityName;
        else
            leftAbilityName.text = "None";

        leftAbilityKeyForeground.text = leftAbilityAction.action.GetBindingDisplayString();
        leftAbilityKeyBackground.text = leftAbilityAction.action.GetBindingDisplayString();
        leftAbilityBackground.fillAmount = 1;

        rightAbilityKeyForeground.text = rightAbilityAction.action.GetBindingDisplayString();
        rightAbilityKeyBackground.text = rightAbilityAction.action.GetBindingDisplayString();
        rightAbilityBackground.fillAmount = 1;
    }

    private void HandleCooldown()
    {
        // left ability
        if (leftCooldownTimer > 0)
        {
            leftCooldownTimer -= Time.deltaTime;

            if (leftCurrentCooldownDuration > 0)
            {
                float elapsedTime = leftCurrentCooldownDuration - leftCooldownTimer;
                float calculatedFillAmount = elapsedTime / leftCurrentCooldownDuration;
                leftAbilityBackground.fillAmount = Mathf.Clamp01(calculatedFillAmount);
            }
        }

        // right ability
        if (rightCooldownTimer > 0)
        {
            rightCooldownTimer -= Time.deltaTime;

            if (rightCurrentCooldownDuration > 0)
            {
                float elapsedTime = rightCurrentCooldownDuration - rightCooldownTimer;
                float calculatedFillAmount = elapsedTime / rightCurrentCooldownDuration;
                rightAbilityBackground.fillAmount = Mathf.Clamp01(calculatedFillAmount);
            }
        }
    }

    public void TriggerCooldown(Abilities abilityUsed)
    {
        // skip cooldown
        if (ServerManager.instance.isTutorialServer || ServerManager.instance.isPracticeServer || !ServerManager.instance.didStartGame.Value)
        {
            if (abilityUsed == leftAbility)
            {
                leftCurrentCooldownDuration = 1;
                leftCooldownTimer = leftCurrentCooldownDuration;
                leftAbilityBackground.fillAmount = 0;
                isLeftAbilityTriggered = false;
                leftAbility.StopAnimation();
            }

            else if (abilityUsed == rightAbility)
            {
                rightCurrentCooldownDuration = 1;
                rightCooldownTimer = rightCurrentCooldownDuration;
                rightAbilityBackground.fillAmount = 0;
                isRightAbilityTriggered = false;
                rightAbility.StopAnimation();
            }

            return;
        }

        if (abilityUsed == leftAbility)
        {
            leftCurrentCooldownDuration = leftAbility.cooldownTime;
            leftCooldownTimer = leftCurrentCooldownDuration;
            leftAbilityBackground.fillAmount = 0; 
            isLeftAbilityTriggered = false;
            leftAbility.StopAnimation();
        }

        else if (abilityUsed == rightAbility)
        {
            rightCurrentCooldownDuration = rightAbility.cooldownTime;
            rightCooldownTimer = rightCurrentCooldownDuration;
            rightAbilityBackground.fillAmount = 0;
            isRightAbilityTriggered = false;
            rightAbility.StopAnimation();
        }
    }

    public void TriggerCooldown(Abilities abilityUsed, float newCooldown)
    {
        // skip cooldown
        if (ServerManager.instance.isTutorialServer || ServerManager.instance.isPracticeServer || !ServerManager.instance.didStartGame.Value)
        {
            if (abilityUsed == leftAbility)
            {
                leftCurrentCooldownDuration = 1;
                leftCooldownTimer = leftCurrentCooldownDuration;
                leftAbilityBackground.fillAmount = 0;
                isLeftAbilityTriggered = false;
                leftAbility.StopAnimation();
            }

            else if (abilityUsed == rightAbility)
            {
                rightCurrentCooldownDuration = 1;
                rightCooldownTimer = rightCurrentCooldownDuration;
                rightAbilityBackground.fillAmount = 0;
                isRightAbilityTriggered = false;
                rightAbility.StopAnimation();
            }

            return;
        }

        // normal cooldown logic
        if (abilityUsed == leftAbility)
        {
            leftCurrentCooldownDuration = newCooldown;
            leftCooldownTimer = leftCurrentCooldownDuration;
            leftAbilityBackground.fillAmount = 0;
            isLeftAbilityTriggered = false;
            leftAbility.StopAnimation();
        }

        else if (abilityUsed == rightAbility)
        {
            rightCurrentCooldownDuration = newCooldown;
            rightCooldownTimer = rightCurrentCooldownDuration;
            rightAbilityBackground.fillAmount = 0;
            isRightAbilityTriggered = false;
            rightAbility.StopAnimation();
        }
    }

    private void PerformLeftAbility()
    {
        if (leftCooldownTimer > 0 || isLeftAbilityTriggered)
            return;

        leftAbility.Activate(gameObject);
        isLeftAbilityTriggered = true;
    }

    private void PerformRightAbility()
    {
        if (rightCooldownTimer > 0 || isRightAbilityTriggered)
            return;

        rightAbility.Activate(gameObject);
        isRightAbilityTriggered = true;
    }

    public bool IsAbilityActivitated() => isLeftAbilityTriggered || isRightAbilityTriggered; 
}
