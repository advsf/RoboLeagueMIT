using UnityEngine;
using Unity.Netcode;

/* tutorial stages:

0. movement(wasd, sprinting, sliding, dashing, and jumping)
1. dribbling
2. shooting(also tell user to flick it to add height or make it spin)
3. powerkick
4. speedster
5. trap
6. roulette
7. kick
8. deflect
9. goalkeeping (diving, catching, drop kicking, and rolling) 

 */

public class TutorialManager : NetworkBehaviour
{
    public static TutorialManager instance;

    [Header("Spawn References")]
    [SerializeField] private Transform startingSpawnPoint;
    [SerializeField] private Transform dribblingMainBallSpawnPoint; // dribbling stage main ball start pos
    [SerializeField] private Transform emptyShootingNetSpawnPoint;

    [Header("Powerkick Stage Spawn References")]
    [SerializeField] private Transform powerkickShootingBallSpawnPoint;
    [SerializeField] private Transform powerkickPlayerSpawnPoint;

    [Header("Trap Stage Spawn References")]
    [SerializeField] private Transform trapBallSpawnPoint;
    [SerializeField] private Transform trapPlayerSpawnPoint;

    [Header("Goalkeeper Stage Spawn References")]
    [SerializeField] private Transform goalkeeperPlayerSpawnPoint;
    [SerializeField] private Transform goalkeeperDiveBallSpawnPoint;
    [SerializeField] private Transform goalkeeperCatchBallSpawnPoint;

    [Header("Stage References")]
    [SerializeField] private GameObject[] tutorialObjs;

    public int currentTutorialStageNumber = -1;

    private void Start()
    {
        instance = this;

        StartTutorial();
    }

    public override void OnNetworkDespawn()
    {
        instance = null;

        base.OnNetworkDespawn();
    }

    public void StartTutorial()
    {
        // spawn with the movement guide
        PlayerInfo.instance.StartTutorialPlayer(startingSpawnPoint, PlayerDataTypes.Team.Blue, PlayerDataTypes.Position.CF, false);

        MoveToNextTutorialStep();
    }

    public void MoveToNextTutorialStep()
    {
        currentTutorialStageNumber++;

        foreach (GameObject stage in tutorialObjs)
            stage.SetActive(false);

        tutorialObjs[currentTutorialStageNumber].SetActive(true);

        BallManager.instance.DeleteNonMainBalls();

        // handle stage-related settings
        switch (currentTutorialStageNumber)
        {
            // movement
            case 0:
                PlayerMovement.instance.RotatePlayer(new(0, -90, 0));
                break;

            // dribbling
            case 1:
                BallManager.instance.RequestBallSpawnServerRpc(dribblingMainBallSpawnPoint.position);

                PlayerMovement.instance.TeleportPlayerToPosition(startingSpawnPoint.position);
                PlayerMovement.instance.RotatePlayer(new(0, -90, 0));
                break;

            // shooting
            case 2:
                BallManager.instance.RequestBallSpawnServerRpc(dribblingMainBallSpawnPoint.position);

                PlayerMovement.instance.TeleportPlayerToPosition(emptyShootingNetSpawnPoint.position);
                PlayerMovement.instance.RotatePlayer(new(0, 90, 0));
                break;
            // powerkick
            case 3:
                BallManager.instance.RequestBallSpawnServerRpc(powerkickShootingBallSpawnPoint.position);

                PlayerInfo.instance.StartTutorialPlayer(powerkickPlayerSpawnPoint, PlayerDataTypes.Team.Blue, PlayerDataTypes.Position.CF, true);
                PlayerMovement.instance.RotatePlayer(new(0, 90, 0));
                break;
            // speedster
            case 4:
                BallManager.instance.RequestBallSpawnServerRpc(dribblingMainBallSpawnPoint.position);

                PlayerMovement.instance.TeleportPlayerToPosition(startingSpawnPoint.position);
                PlayerMovement.instance.RotatePlayer(new(0, -90, 0));
                break;
            // trap
            case 5:
                BallManager.instance.RequestBallSpawnServerRpc(trapBallSpawnPoint.position);

                PlayerInfo.instance.StartTutorialPlayer(trapPlayerSpawnPoint, PlayerDataTypes.Team.Blue, PlayerDataTypes.Position.LMF, true);
                PlayerMovement.instance.RotatePlayer(new(0, -180, 0));
                break;
            // roulette
            case 6:
                BallManager.instance.RequestBallSpawnServerRpc(dribblingMainBallSpawnPoint.position);

                PlayerMovement.instance.TeleportPlayerToPosition(startingSpawnPoint.position);
                PlayerMovement.instance.RotatePlayer(new(0, -90, 0));
                break;
            // kick
            case 7:
                BallManager.instance.RequestBallSpawnServerRpc(dribblingMainBallSpawnPoint.position);

                PlayerInfo.instance.StartTutorialPlayer(startingSpawnPoint, PlayerDataTypes.Team.Blue, PlayerDataTypes.Position.CB, true);
                PlayerMovement.instance.RotatePlayer(new(0, -90, 0));
                break;
            // deflect
            case 8:
                BallManager.instance.RequestBallSpawnServerRpc(trapBallSpawnPoint.position);

                PlayerInfo.instance.StartTutorialPlayer(trapPlayerSpawnPoint, PlayerDataTypes.Team.Blue, PlayerDataTypes.Position.CB, true);
                PlayerMovement.instance.RotatePlayer(new(0, -180, 0));
                break;
            // gk dive
            case 9:
                BallManager.instance.RequestBallSpawnServerRpc(goalkeeperDiveBallSpawnPoint.position);

                ServerManager.instance.RequestChangeOfPositionServerRpc(PlayerDataTypes.Team.Blue, PlayerDataTypes.Position.GK);
                PlayerMovement.instance.RotatePlayer(new(0, -90, 0));
                break;
            // goalkeeper catch, drop kick, roll
            case 10:
                BallManager.instance.RequestBallSpawnServerRpc(goalkeeperCatchBallSpawnPoint.position);

                PlayerMovement.instance.TeleportPlayerToPosition(goalkeeperCatchBallSpawnPoint.position);
                PlayerMovement.instance.RotatePlayer(new(0, -90, 0));
                break;
        }
    }

    public void PassToNextDetectorInSameStage()
    {
        tutorialObjs[currentTutorialStageNumber].GetComponent<HandleTutorialPlayerDetectors>().MoveToNextDetector();
    }
}