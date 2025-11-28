using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class HandleBarrierBallDetection : NetworkBehaviour
{
    public static bool isOutOfBounds;

    [Header("Settings")]
    [SerializeField] private bool isBlueGoalLineDetector;
    [SerializeField] private bool isRedGoalLineDetector;
    [SerializeField] private float sidelineZSpawnPos;
    public float restartPlayDelay;
    public bool isActive;

    [Header("Spawn References")]
    public Transform center;
    public Transform blueGoalKickSpawnPoint;
    public Transform redGoalKickSpawnPoint;
    public Transform leftCorner;
    public Transform rightCorner;
    public Vector3 ballHitPos;
    public Vector3 boundaryPos;

    // this is only used for sideline outs
    [Header("Throw In Positon Boundary References")]
    [SerializeField] private bool shouldRotate180Degrees;
    [SerializeField] private float positionBoundaryZPos;
    [SerializeField] private GameObject positionBoundary;

    [Header("Goalkick Boundary References")]
    [SerializeField] private GameObject blueTeamGoalkickBoundaryObj;
    [SerializeField] private GameObject redTeamGoalkickBoundaryObj;
     
    [Header("References")]
    [SerializeField] private Rigidbody ballRb;
    [SerializeField] private BallSync ballSync;

    private Vector3 hitPos;
    private int lastKicker;

    private string possessionTeam = "";

    private void Start()
    {
        blueTeamGoalkickBoundaryObj.SetActive(false);
        redTeamGoalkickBoundaryObj.SetActive(false);
    }

    private void OnTriggerStay(Collider other)
    {
        // one checks for the ball locally and one is network
        // meaning that the former is to prevent multiple occurences of the ball being detected
        // and the latter is to check if the ball has been thrown back into play
        // and the final one is to check if the ball has been scored
        if (!IsServer || isOutOfBounds || ballSync.isOutOfBounds.Value || ServerManager.instance.didATeamScore.Value)
            return;

        // if the game did not start yet, do nothing, let the ball fly off.
        if (!ServerManager.instance.didStartGame.Value)
            return;

        if (other.transform.CompareTag("Ball"))
        {
            hitPos = ballRb.position;
            lastKicker = ballSync.lastKickedClientId.Value;

            // if goalkeeper kicks it out somehow
            if (lastKicker < 0)
            {
                isOutOfBounds = true;

                // this is for the server to know which detector script to use
                ServerManager.instance.DetermineWhichDetectorWasUsed(this);

                boundaryPos = new(hitPos.x, 5f, positionBoundaryZPos);
                ballHitPos = new(hitPos.x, 2f, sidelineZSpawnPos);

                StartCoroutine(HandleSideOutOfBoundsPlay(ballHitPos, lastKicker == -2 ? "Blue" : "Red"));

                return;
            }

            isActive = true;

            if (!ServerManager.instance.didStartGame.Value)
                ballSync.ResetBallServerRpc(center.position);

            else
                StartOutOfBoundsPlay();
        }
    }

    private void StartOutOfBoundsPlay()
    {
        // prevent multiple occurences of the ball getting detected out of bounds
        isOutOfBounds = true;

        // this is for the server to know which detector script to use
        ServerManager.instance.DetermineWhichDetectorWasUsed(this);

        // handle which team gets possession
        if (NetworkManager.ConnectedClients[(ulong)lastKicker].PlayerObject.GetComponent<PlayerInfo>().currentTeam.Value.Equals("Blue"))
            possessionTeam = "Red";

        else
            possessionTeam = "Blue";

        // this checks the game should resume in a corner kick
        if ((isBlueGoalLineDetector && NetworkManager.ConnectedClients[(ulong)lastKicker].PlayerObject.GetComponent<PlayerInfo>().currentTeam.Value.Equals("Blue"))
            || (isRedGoalLineDetector && NetworkManager.ConnectedClients[(ulong)lastKicker].PlayerObject.GetComponent<PlayerInfo>().currentTeam.Value.Equals("Red")))
        {
            ballSync.isCornerKick.Value = true;

            // determine which corner to use
            // right corner
            if (hitPos.z <= -14.5)
                StartCoroutine(HandleGoalLineOutOfBoundsPlay(false, rightCorner.position));
            else
                StartCoroutine(HandleGoalLineOutOfBoundsPlay(false, leftCorner.position));
        }

        // goalkicks
        else if (isBlueGoalLineDetector)
        {
            StartCoroutine(HandleGoalLineOutOfBoundsPlay(true, blueGoalKickSpawnPoint.position));
        }
            

        else if (isRedGoalLineDetector)
        {
            StartCoroutine(HandleGoalLineOutOfBoundsPlay(true, redGoalKickSpawnPoint.position));
        }
            
        // sideline out
        else
        {
            boundaryPos = new(hitPos.x, 5f, positionBoundaryZPos);
            ballHitPos = new(hitPos.x, 2f, sidelineZSpawnPos);

            StartCoroutine(HandleSideOutOfBoundsPlay(ballHitPos, possessionTeam));
        }
    }

    public IEnumerator HandleGoalLineOutOfBoundsPlay(bool isGoalKick, Vector3 newPos)
    {
        yield return new WaitForEndOfFrame();

        PlayAndSyncWhistleSound();

        yield return new WaitForSeconds(restartPlayDelay);

        if (ServerManager.instance.isGameOver.Value)
        {
            isOutOfBounds = false;
            yield break;
        }

        // if corner kick
        if (!isGoalKick)
            ServerManager.instance.HandleWhichTeamHasPossessionInOutOfBoundsInServerRpc(false, possessionTeam);

        // enable goalkicks
        if (isGoalKick)
        {
            ServerManager.instance.HandleGoalkickPossessionsServerRpc(possessionTeam);

            ballSync.isOutOfBounds.Value = true;
            EnableGoalkickBoundary(possessionTeam.Equals("Blue"), true);
        }

        ServerManager.instance.ResetOutOfBoundsTimer();

        isOutOfBounds = false;
        ballSync.ResetBallServerRpc(newPos);
    }

    public IEnumerator HandleGoalLineOutOfBoundsPlay(bool isGoalKick, Vector3 newPos, string newPossessionTeam)
    {
        yield return new WaitForEndOfFrame();

        PlayAndSyncWhistleSound();

        yield return new WaitForSeconds(restartPlayDelay);

        if (ServerManager.instance.isGameOver.Value)
        {
            isOutOfBounds = false;
            yield break;
        }

        // if corner kick
        if (!isGoalKick)
            ServerManager.instance.HandleWhichTeamHasPossessionInOutOfBoundsInServerRpc(false, newPossessionTeam);

        // enable goalkicks
        if (isGoalKick)
        {
            ServerManager.instance.HandleGoalkickPossessionsServerRpc(newPossessionTeam);

            ballSync.isOutOfBounds.Value = true;
            EnableGoalkickBoundary(newPossessionTeam.Equals("Blue"), true);
        }

        ServerManager.instance.ResetOutOfBoundsTimer();

        isOutOfBounds = false;
        ballSync.ResetBallServerRpc(newPos);
    }

    public IEnumerator HandleSideOutOfBoundsPlay(Vector3 newPos, string teamWithPossession)
    {
        yield return new WaitForEndOfFrame();

        PlayAndSyncWhistleSound();

        MovePositionBoundary(boundaryPos);
        MovePositionBoundaryClientRpc(boundaryPos, shouldRotate180Degrees);

        yield return new WaitForSeconds(restartPlayDelay);

        if (ServerManager.instance.isGameOver.Value)
        {
            isOutOfBounds = false;
            yield break;
        }

        ServerManager.instance.ResetOutOfBoundsTimer();
        ServerManager.instance.HandleWhichTeamHasPossessionInOutOfBoundsInServerRpc(true, teamWithPossession);

        isOutOfBounds = false;
        ballSync.ResetBallServerRpc(newPos);
    }

    #region Whistle Sound
    private void PlayAndSyncWhistleSound()
    {
        SoundManager.instance.PlayWhistleSound();

        SyncWhistleSoundClientRpc();
    }


    [ClientRpc]
    private void SyncWhistleSoundClientRpc()
    {
        if (IsHost)
            return;

        SoundManager.instance.PlayWhistleSound();
    }

    #endregion

    #region Position Boundary
    public void MovePositionBoundary(Vector3 pos)
    {
        positionBoundary.transform.position = pos;

        if (shouldRotate180Degrees)
            positionBoundary.transform.rotation = Quaternion.Euler(new(0, 180f, 0));

        else
            positionBoundary.transform.rotation = Quaternion.identity;
    }

    [ClientRpc]
    private void MovePositionBoundaryClientRpc(Vector3 pos, bool shouldRotate180Degrees)
    {
        // no need to do this again for the host
        if (IsHost)
            return;

        positionBoundary.transform.position = pos;

        if (shouldRotate180Degrees)
            positionBoundary.transform.rotation = Quaternion.Euler(new(0, 180f, 0));

        else
            positionBoundary.transform.rotation = Quaternion.identity;
    }

    #endregion

    #region Goalkick Boundary

    public void EnableGoalkickBoundary(bool isBlueNet, bool condition)
    {
        Debug.Log(isBlueNet + "124");

        if (isBlueNet)
            EnableBlueGoalkickBoundaryClientRpc(condition);
        else
            EnableRedGoalkickBoundaryClientRpc(condition);
    }

    [ClientRpc]
    private void EnableBlueGoalkickBoundaryClientRpc(bool condition)
    {
        blueTeamGoalkickBoundaryObj.SetActive(condition);
    }

    [ClientRpc]
    private void EnableRedGoalkickBoundaryClientRpc(bool condition)
    {
        redTeamGoalkickBoundaryObj.SetActive(condition);
    }

    #endregion

}
