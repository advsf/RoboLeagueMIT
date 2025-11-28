using UnityEngine;
using Unity.Netcode;

public class HandleNetBallDetectors : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody ballRb;
    [SerializeField] private BallSync ballSync;
    [SerializeField] private Transform ownGoalballPos;

    [Header("Setting")]
    [SerializeField] private bool isBlueNet;
    [SerializeField] private bool isRedNet;
    [SerializeField] private float crowdSoundDelay = 0.4f;
    [SerializeField] private float goalScoreUIDelay = 0.1f;
    
    private PlayerInfo kickerInfo;
    private PlayerInfo secondKickerInfo;

    private int kickerId;
    private int secondLastKickerId;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        ServerManager.instance.didATeamScore.Value = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || !other.CompareTag("Ball") || !ServerManager.instance.didStartGame.Value)
            return;

        HandleScoring();
    }

    private void HandleScoring()
    {
        // avoid repeating or scoring when the game is finished
        if (ServerManager.instance.didATeamScore.Value || ServerManager.instance.isGameOver.Value)
            return;

        // if we haven't started the game
        if (!ServerManager.instance.didStartGame.Value)
        {
            ServerManager.instance.ResetBallToTheCenter();
            return;
        }

        ServerManager.instance.didATeamScore.Value = true;

        // get the ids
        kickerId = ballSync.lastKickedClientId.Value;

        secondLastKickerId = ballSync.secondLastKickedClientId.Value;

        // get the playerinfos
        if (kickerId >= 0 && NetworkManager.Singleton.ConnectedClients.ContainsKey((ulong)kickerId))
        {
            kickerInfo = NetworkManager.Singleton.ConnectedClients[(ulong)kickerId].PlayerObject.GetComponent<PlayerInfo>();
            ballSync.scorerUsername.Value = kickerInfo.username.Value.ToString();
        }

        if (secondLastKickerId >= 0 && NetworkManager.Singleton.ConnectedClients.ContainsKey((ulong)secondLastKickerId))
        {
            secondKickerInfo = NetworkManager.Singleton.ConnectedClients[(ulong)secondLastKickerId].PlayerObject.GetComponent<PlayerInfo>();

            if (secondKickerInfo.currentTeam.Value.Equals(kickerInfo.currentTeam.Value))
                ballSync.assisterUsername.Value = secondKickerInfo.username.Value.ToString();
        }

        // if we scored an own goal
        // dont count it, and just tp the ball forward
        if (kickerInfo != null && ((isBlueNet && kickerInfo.currentTeam.Value.Equals("Blue")) || (isRedNet && kickerInfo.currentTeam.Value.Equals("Red"))))
        {
            ballSync.ResetBallServerRpc(ownGoalballPos.position);

            ServerManager.instance.didATeamScore.Value = false;

            return;
        }

        // if we scored
        else
        {
            // increment the goal count
            if (kickerInfo != null)
                kickerInfo.goals.Value++;

            // when we couldn't find the info of the kicker
            else
            {
                ballSync.ResetBallServerRpc(ownGoalballPos.position);

                ServerManager.instance.didATeamScore.Value = false;

                return;
            }

            // if the second kicker assisted the first kicker
            if (secondKickerInfo != null && secondKickerInfo.currentTeam.Value.Equals(kickerInfo.currentTeam.Value))
                secondKickerInfo.assists.Value++;

            // play sound effect
            PlayGoalSound();

            Invoke(nameof(PlayGoalUIAnimation), goalScoreUIDelay);

            // ask the server to reset the game
            if (kickerInfo != null)
                Invoke(nameof(RestartRound), 7);
        }
    }

    private void PlayGoalUIAnimation()
    {
        HandleVariousUIAnimationsInGame.instance.PlayGoalScoreUIAnimation();

        PlayGoalScoreUIAnimationClientRpc();
    }

    private void RestartRound()
    {
        ServerManager.instance.RestartRound(kickerInfo.currentTeam.Value.ToString());
    }

    private void PlayGoalSound()
    {
        SoundManager.instance.PlayWhistleSound();
        Invoke(nameof(PlayCrowdCheerSound), crowdSoundDelay);

        PlayScoreSoundEffectClientRpc();
    }

    [ClientRpc]
    private void PlayGoalScoreUIAnimationClientRpc()
    {
        if (IsHost)
            return;

        HandleVariousUIAnimationsInGame.instance.PlayGoalScoreUIAnimation();
    }

    [ClientRpc]
    private void PlayScoreSoundEffectClientRpc()
    {
        if (IsHost)
            return;

        SoundManager.instance.PlayWhistleSound();
        Invoke(nameof(PlayCrowdCheerSound), crowdSoundDelay);
    }

    private void PlayCrowdCheerSound() => SoundManager.instance.PlayCheerCrowdSound();
}
