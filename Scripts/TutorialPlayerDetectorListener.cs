using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Collections;

public class TutorialPlayerDetectorListener : NetworkBehaviour
{
    [SerializeField] private HandleTutorialPlayerDetectors tutorialDetector;
    
    [Header("Ball Detection Settings")]
    [SerializeField] private bool shouldDetectBall = false;

    [Header("Spawn Ball Settings")]
    [SerializeField] private bool shouldSpawnBall = false;
    [SerializeField] private bool shouldSpawnLocalBall = false;
    [SerializeField] private bool shouldEnableMainBall = false; // for the catch stage because of the way we handle catch ball animation
    [SerializeField] private BallSync mainBallSync;
    [SerializeField] private Transform ballSpawnPlace;

    [Header("Ball Launching Settings")]
    [SerializeField] private bool shouldLaunchBall = false;
    [SerializeField] private bool randomizeBallLaunchDirectionOnTheZAxis = false;
    [SerializeField] private Vector3 ballLaunchDirection;
    [SerializeField] private float randomizeBallLaunchDirectionAmount = 0.1f;
    [SerializeField] private float forwardBallLaunchForce;
    [SerializeField] private float upwardBallLaunchForce;

    [Header("Ability/Input Requirement Settings")]
    [SerializeField] private bool shouldUserUseAbilityToPass = false;
    [SerializeField] private bool shouldUserOnlyUseAbilityToPass = false;
    [SerializeField] private bool shouldBallBeKinematicForAbilityToCount = false;
    public InputActionReference abilityAction;
    public bool isAbilityPressed = false;

    [Header("Player Teleport Settings")]
    [SerializeField] private bool shouldTeleportPlayer = false;
    [SerializeField] private Transform teleportPlace;

    [Header("Tutorial Bot Settings")]
    [SerializeField] private bool shouldEnableBot = false;
    [SerializeField] private GameObject tutorialBotObj;
    [SerializeField] private Transform tutorialBotSpawnPlace;

    [Header("Transition Settings")]
    [SerializeField] private float timeBeforeNextDetector = 0;

    [Header("UI Settings")]
    public bool shouldOpenBeginningTutorialUI = false; // ui that is for introducting key skills at the beginning that disables the movement
    [SerializeField] private bool shouldStartNewObjectiveUI = true;
    [SerializeField] private GameObject tutorialUI;

    public bool isHit = false;

    private void OnEnable()
    {
        HandleBallSpawning();

        HandleTutorialBotSpawning();

        // teleport player
        if (shouldTeleportPlayer)
            PlayerMovement.instance.TeleportPlayerToPosition(teleportPlace.position);
    }

    private void Update()
    {
        // if using an ability is a requirement to pass
        if (shouldUserUseAbilityToPass)
        {
            if (abilityAction.action.WasPressedThisFrame() && !tutorialUI.activeInHierarchy)
            {
                if (!shouldBallBeKinematicForAbilityToCount || (shouldBallBeKinematicForAbilityToCount && BallManager.instance.FindNearestBall(transform.position).GetRigidbody().isKinematic))
                    isAbilityPressed = true;
            }
        }

        // if we only need to use an ability to pass
        if (shouldUserOnlyUseAbilityToPass)
        {
            if (abilityAction.action.WasPressedThisFrame())
            {
                tutorialDetector.AdvanceToNextDetector(timeBeforeNextDetector);
                HandleTutorialObjectiveUI.instance.UpdateTutorialObjectiveUI();
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            // if the user has to reset the ball,
            // make them do the ability again
            isAbilityPressed = false;

            // launch ball
            if (shouldLaunchBall)
            {
                HandleKicking.instance.ResetLocalBallPosition(ballSpawnPlace.position);
                BallManager.instance.GetLocalSpawnedBall(NetworkManager.LocalClientId).GetRigidbody().AddForce((randomizeBallLaunchDirectionOnTheZAxis 
                    ? new(ballLaunchDirection.x, ballLaunchDirection.y, Random.Range(-randomizeBallLaunchDirectionAmount, randomizeBallLaunchDirectionAmount)) : ballLaunchDirection)
                    * forwardBallLaunchForce + Vector3.up * upwardBallLaunchForce, ForceMode.Impulse);
            }

            // spawn the ball regularly (to the player's position)
            else
            {
                if (HandleKicking.instance.IsLocalBallSpawned())
                    HandleKicking.instance.ResetLocalBallPosition(PlayerMovement.instance.transform.position);
                else
                    BallManager.instance.RequestBallSpawnServerRpc(PlayerMovement.instance.transform.position);
            }
        }
    }

    public void OpenTutorialUI()
    {
        PlayerMovement.instance.DisableMovement(true);

        tutorialUI.SetActive(true);
    }

    public void ContinueOnTutorial()
    {
        tutorialUI.SetActive(false);

        HandleCursorSettings.instance.EnableCursor(false, true);

        // show the objective UI
        if (shouldStartNewObjectiveUI)
            HandleTutorialObjectiveUI.instance.EnableTutorialObjectiveUI();

        // launch ball
        if (shouldLaunchBall)
            BallManager.instance.GetLocalSpawnedBall(NetworkManager.LocalClientId).GetRigidbody().AddForce((randomizeBallLaunchDirectionOnTheZAxis
                    ? new(ballLaunchDirection.x, ballLaunchDirection.y, Random.Range(-randomizeBallLaunchDirectionAmount, randomizeBallLaunchDirectionAmount)) : ballLaunchDirection)
                    * forwardBallLaunchForce + Vector3.up * upwardBallLaunchForce, ForceMode.Impulse);

        // allow movement again
        PlayerMovement.instance.DisableMovement(false);
    }

    private void HandleBallSpawning()
    {
        if (shouldSpawnBall)
            BallManager.instance.SpawnNonPlayerOwnedBall(ballSpawnPlace.position);

        // local ball
        if (shouldSpawnLocalBall)
            if (HandleKicking.instance.IsLocalBallSpawned())
                HandleKicking.instance.ResetLocalBallPosition(ballSpawnPlace.position);

            else
                BallManager.instance.RequestBallSpawnServerRpc(ballSpawnPlace.position);

        // main ball
        if (shouldEnableMainBall)
        {
            mainBallSync.transform.root.gameObject.SetActive(true);
            mainBallSync.ResetBallServerRpc(ballSpawnPlace.position);
        }

        // launch ball
        if (shouldLaunchBall)
        {
            Rigidbody rb = BallManager.instance.GetLocalSpawnedBall(NetworkManager.LocalClientId).GetRigidbody();

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            BallManager.instance.GetLocalSpawnedBall(NetworkManager.LocalClientId).GetRigidbody().AddForce((randomizeBallLaunchDirectionOnTheZAxis
                    ? new(ballLaunchDirection.x, ballLaunchDirection.y, Random.Range(-randomizeBallLaunchDirectionAmount, randomizeBallLaunchDirectionAmount)) : ballLaunchDirection)
                    * forwardBallLaunchForce + Vector3.up * upwardBallLaunchForce, ForceMode.Impulse);
        }
    }

    private void HandleTutorialBotSpawning()
    {
        if (shouldEnableBot)
        {
            tutorialBotObj.SetActive(true);
            tutorialBotObj.transform.position = tutorialBotSpawnPlace.position;
        }

        else if (tutorialBotObj != null)
            tutorialBotObj.SetActive(false);
    }

    public IEnumerator PassToNextDectector()
    {
        if (isHit)
            yield break;

        if (shouldUserUseAbilityToPass)
        {
            // small delay so that we give the computer time to realize that the ability has been pressed
            yield return new WaitForSeconds(0.025f);

            if (!isAbilityPressed)
                yield break;
        }

        isHit = true;

        tutorialDetector.AdvanceToNextDetector(timeBeforeNextDetector);
        HandleTutorialObjectiveUI.instance.UpdateTutorialObjectiveUI();
    }

    public void TeleportBallBack()
    {
        if (isHit)
            return;

        HandleBallSpawning();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isHit || (shouldUserUseAbilityToPass && !isAbilityPressed))
            return;

        // detect player
        if (!shouldDetectBall)
        {
            if (other.GetComponent<PlayerMovement>())
                StartCoroutine(PassToNextDectector());
        }    

        // detect ball    
        else if (other.CompareTag("Ball"))
            StartCoroutine(PassToNextDectector());
    }

}
