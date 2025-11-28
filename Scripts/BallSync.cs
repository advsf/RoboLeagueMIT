using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

public class BallSync : NetworkBehaviour
{
    private Rigidbody ballRb;
    private NetworkObject _networkObject;

    private struct StateSnapshot : INetworkSerializable
    {
        public int Tick;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public Vector3 AngularVelocity;
        public float KickNetworkTime;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Tick);
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Rotation);
            serializer.SerializeValue(ref Velocity);
            serializer.SerializeValue(ref AngularVelocity);
            serializer.SerializeValue(ref KickNetworkTime);
        }
    }

    public struct InputPayload : INetworkSerializable
    {
        public int Tick;
        public Vector3 Force;
        public Vector3 AngularImpulse;
        public bool StopBallFirst;
        public bool SlideKick;

        public Vector3 ClientBallPosition;
        public Vector3 ClientBallVelocity;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Tick);
            serializer.SerializeValue(ref Force);
            serializer.SerializeValue(ref AngularImpulse);
            serializer.SerializeValue(ref StopBallFirst);
            serializer.SerializeValue(ref SlideKick);
            serializer.SerializeValue(ref ClientBallPosition);
            serializer.SerializeValue(ref ClientBallVelocity);
        }
    }

    private struct ContestedKickRequest
    {
        public InputPayload Payload;
        public ulong SenderId;
    }

    private List<ContestedKickRequest> _contestedKicks;

    [Header("Visual Smoothing")]
    [SerializeField] private Transform visualBallTransform;
    private Vector3 _visualVelocity;
    private Quaternion _visualAngularVelocity;

    [Header("Interpolation Settings")]
    [SerializeField] private float interpolationTime = 0.045f;
    [SerializeField] private float ballInAnimationInterpolationTime = 0.045f;

    [Header("Ball Physics")]
    [SerializeField] private float magnusForceMultiplier = 0.05f;
    [SerializeField] private float downForceMultiplier = 1.0f;
    [SerializeField] private float magnusEffectDuration = 2.0f;
    [SerializeField] private float magnusDecayRate = 0.98f;
    private float _kickTime = -1f;

    [Header("Server Validation")]
    [SerializeField] private float maxLegalForce = 54.6f;
    [SerializeField] private SphereCollider ballCollider;

    [Header("Server-Side Arbitration")]
    [SerializeField] private float kickCooldownDuration = 0.1f;
    private bool _isKickOnCooldown = false;

    private NetworkVariable<StateSnapshot> _serverState = new NetworkVariable<StateSnapshot>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> isBallPickedUp = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isOutOfBounds = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isThrowIn = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isCornerKick = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isGoalKick = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> lastKickedClientId = new(-1);
    public NetworkVariable<int> secondLastKickedClientId = new(-1);
    public NetworkVariable<FixedString32Bytes> lastKickedTeam = new(string.Empty, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString32Bytes> scorerUsername = new(string.Empty, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString32Bytes> assisterUsername = new(string.Empty, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        ballRb = GetComponent<Rigidbody>();
        _networkObject = GetComponent<NetworkObject>();

        BallManager.instance.RegisterBall(this);
        DeterministicPhysicsSetup.ConfigureRigidbodyForDeterminism(ballRb);

        if (!IsServer)
            _serverState.OnValueChanged += OnServerStateChanged;

        else
            _contestedKicks = new List<ContestedKickRequest>();

        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        BallManager.instance.UnregisterBall(this);

        if (!IsServer)
            _serverState.OnValueChanged -= OnServerStateChanged;

        base.OnNetworkDespawn();
    }

    private void Start()
    {
        if (!IsServer)
        {
            // client does not simulate gravity
            ballRb.useGravity = false;
        }
    }

    private void LateUpdate()
    {
        // no interpolation when the ball is kinematic or out of bounds (mainly for the throw in and goalkeeper animation)
        if (isOutOfBounds.Value || ballRb.isKinematic)
        {
            visualBallTransform.position = transform.position;
            visualBallTransform.rotation = transform.rotation;
            _visualVelocity = Vector3.zero;
        }

        // smooth interpolation for both the host and the client
        else
        {
            visualBallTransform.position = Vector3.SmoothDamp(
                    visualBallTransform.position,
                    transform.position,
                    ref _visualVelocity,
                    interpolationTime);

            // just set the rotation directly - do NOT interpolate rotations
            visualBallTransform.rotation = transform.rotation;
        }
    }

    private void FixedUpdate()
    {
        if (!IsSpawned)
            return;

        // only the server should simulate physics to prevent desyncs
        if (IsServer)
        {
            ProcessContestedKicks();

            if (!ballRb.isKinematic)
            {
                HandleSpinDecay();
                ApplySharedPhysics();
                _serverState.Value = GetCurrentState();
            }
        }
    }

    public void LocalKick(InputPayload kickPayload, int kickerId)
    {
        if (kickPayload.SlideKick && !CanSlideKick())
            return;

        // populate client's ball state for server validation
        kickPayload.ClientBallPosition = ballRb.position;
        kickPayload.ClientBallVelocity = ballRb.linearVelocity;

        if (IsServer)
        {
            // arbitration system
            _contestedKicks.Add(new ContestedKickRequest
            {
                Payload = kickPayload,
                SenderId = (ulong)kickerId
            });
        }

        else
        {
            ApplyKickServerRpc(kickPayload);
        }

        if (!ServerManager.instance.didKickOffEnd.Value)
            ServerManager.instance.DisableKickOffBarriers();
    }

    #region State Synchronization

    private void OnServerStateChanged(StateSnapshot previous, StateSnapshot serverState)
    {
        if (ballRb.isKinematic)
            return;

        // update rigidbody with server state
        ballRb.position = serverState.Position;
        ballRb.rotation = serverState.Rotation;
        ballRb.linearVelocity = serverState.Velocity;
        ballRb.angularVelocity = serverState.AngularVelocity;
        _kickTime = serverState.KickNetworkTime;

        Physics.SyncTransforms();
    }

    #endregion

    #region Physics Networking
private void HandleSpinDecay()
    {
        if (_kickTime <= 0f)
            return;

        float currentTime = (float)NetworkManager.ServerTime.Time;

        if (currentTime > _kickTime + magnusEffectDuration)
        {
            ballRb.angularVelocity *= Mathf.Pow(magnusDecayRate, Time.fixedDeltaTime);

            if (ballRb.angularVelocity.sqrMagnitude < 0.01f)
            {
                ballRb.angularVelocity = Vector3.zero;
                _kickTime = -1f;
            }
        }
    }

    private void ApplyKickMechanics(InputPayload input, ulong clientId)
    {
        if (IsForceIllegal(input.Force) || (input.SlideKick && !CanSlideKick()))
            return;

        if (input.StopBallFirst)
            StopBallVelocity();

        ApplyKickForces(input);
        HandleTrackingKickers((int)clientId);

        if (input.AngularImpulse.sqrMagnitude > 0)
            _kickTime = (float)NetworkManager.ServerTime.Time;
    }

    private void ApplyKickForces(InputPayload input)
    {
        if (IsForceIllegal(input.Force))
            return;

        ballRb.AddForce(input.Force, ForceMode.Impulse);
        ballRb.AddTorque(input.AngularImpulse, ForceMode.Impulse);
    }

    private void ApplySharedPhysics()
    {
        if (ballRb.angularVelocity.magnitude > 0.1f && ballRb.linearVelocity.magnitude > 0.1f)
        {
            Vector3 magnusForce = Vector3.Cross(ballRb.angularVelocity, ballRb.linearVelocity) * magnusForceMultiplier;
            ballRb.AddForce(magnusForce, ForceMode.Force);
        }

        ballRb.AddForce(downForceMultiplier * Vector3.down, ForceMode.Force);
    }

    private void StopBallVelocity()
    {
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
    }

    private void ProcessContestedKicks()
    {
        if (_contestedKicks.Count == 0)
            return;

        if (_isKickOnCooldown)
        {
            _contestedKicks.Clear();
            return;
        }

        // the kick with the earliest tick wins, so we ignore the loser's
        ContestedKickRequest winner = _contestedKicks.OrderBy(req => req.Payload.Tick).First();

        _isKickOnCooldown = true;
        Invoke(nameof(ResetKickCooldown), kickCooldownDuration);

        ApplyKickMechanics(winner.Payload, winner.SenderId);

        _contestedKicks.Clear();
    }

    private void HandleTrackingKickers(int clientId)
    {
        if (clientId < -1)
        {
            if (clientId == -2)
                lastKickedClientId.Value = -1;
            secondLastKickedClientId.Value = -1;
        }
        else
        {
            if (lastKickedClientId.Value != clientId)
                secondLastKickedClientId.Value = lastKickedClientId.Value;
            lastKickedClientId.Value = clientId;
            lastKickedTeam.Value = NetworkManager.ConnectedClients[(ulong)clientId].PlayerObject.GetComponent<PlayerInfo>().currentTeam.Value;
        }
    }

    private bool IsForceIllegal(Vector3 force) => force.magnitude > maxLegalForce;
    private bool CanSlideKick() => !isOutOfBounds.Value;

    public void Teleport(Vector3 newPosition, Quaternion newRotation)
    {
        StopBallVelocity();

        visualBallTransform.SetPositionAndRotation(newPosition, newRotation);

        transform.SetPositionAndRotation(newPosition, newRotation);
        ballRb.position = newPosition;
        ballRb.rotation = newRotation;

        Physics.SyncTransforms();
    }

    private StateSnapshot GetCurrentState()
    {
        return new StateSnapshot
        {
            Tick = NetworkManager.ServerTime.Tick,
            Position = ballRb.position,
            Rotation = ballRb.rotation,
            Velocity = ballRb.linearVelocity,
            AngularVelocity = ballRb.angularVelocity,
            KickNetworkTime = _kickTime
        };
    }

    [ServerRpc(RequireOwnership = false)]
    public void ApplyKickServerRpc(InputPayload input, ServerRpcParams rpcParams = default)
    {
        _contestedKicks.Add(new ContestedKickRequest
        {
            Payload = input,
            SenderId = rpcParams.Receive.SenderClientId
        });
    }

    public void StopBall()
    {
        StopBallVelocity();

        if (IsServer)
            StopBallClientRpc();
        else
            StopBallServerRpc();
    }

    private void ResetKickCooldown()
    {
        _isKickOnCooldown = false;
    }

    public void EnableKinematics(bool condition)
    {
        ballRb.isKinematic = condition;

        if (IsServer)
            EnableKinematicsClientRpc(condition);
        else
            EnableKinematicsServerRpc(condition);
    }

    public void EnableCollider(bool condition)
    {
        ballCollider.enabled = condition;

        if (IsServer) 
            EnableColliderClientRpc(condition);
        else 
            EnableColliderServerRpc(condition);
    }

    public Rigidbody GetRigidbody() => ballRb;

    public bool IsMagnusActive() => ballRb != null && ballRb.angularVelocity.magnitude > 0.1f;

    public Vector3 PredictBallPosition(float timeAhead)
    {
        Vector3 predictedPosition = transform.position;
        Vector3 predictedVelocity = ballRb.linearVelocity;
        Vector3 currentAngularVelocity = ballRb.angularVelocity;
        int ticksAhead = Mathf.RoundToInt(timeAhead / Time.fixedDeltaTime);
        float stepTime = Time.fixedDeltaTime;

        for (int i = 0; i < ticksAhead; i++)
        {
            Vector3 magnusForce = Vector3.Cross(currentAngularVelocity, predictedVelocity) * magnusForceMultiplier;
            predictedVelocity += (Vector3.down * downForceMultiplier + magnusForce) * stepTime;
            predictedPosition += predictedVelocity * stepTime;
            currentAngularVelocity *= (1 - ballRb.angularDamping * stepTime);
        }

        return predictedPosition;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ForceBallStateServerRpc(Vector3 pos, Quaternion rot)
    {
        ForceBallStateClientRpc(pos, rot);
    }

    [ClientRpc]
    public void ForceBallStateClientRpc(Vector3 pos, Quaternion rot)
    {
        Teleport(pos, rot);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetBallServerRpc(Vector3 newPosition)
    {
        Teleport(newPosition, Quaternion.identity);
        SendBallResetToClientsClientRpc(newPosition);
    }

    [ClientRpc]
    private void SendBallResetToClientsClientRpc(Vector3 newPosition)
    {
        if (!IsServer) Teleport(newPosition, Quaternion.identity);
    }

    [ServerRpc(RequireOwnership = false)]
    public void StopBallServerRpc()
    {
        StopBallClientRpc();
    }

    [ClientRpc]
    private void StopBallClientRpc()
    {
        StopBallVelocity();
    }

    [ServerRpc(RequireOwnership = false)]
    private void EnableKinematicsServerRpc(bool condition)
    {
        EnableKinematicsClientRpc(condition);
    }

    [ClientRpc]
    private void EnableKinematicsClientRpc(bool condition)
    {
        ballRb.isKinematic = condition;
    }

    [ServerRpc(RequireOwnership = false)]
    private void EnableColliderServerRpc(bool condition)
    {
        EnableColliderClientRpc(condition);
    }

    [ClientRpc]
    private void EnableColliderClientRpc(bool condition)
    {
        if (!IsServer)
            ballCollider.enabled = condition;
    }

    #endregion

    #region Out of Bounds Handling

    [ServerRpc(RequireOwnership = false)]
    public void EndOutOfBoundsPlayServerRpc()
    {
        isOutOfBounds.Value = false;
        isThrowIn.Value = false;
        isCornerKick.Value = false;
        isBallPickedUp.Value = false;
        isGoalKick.Value = false;

        ServerManager.instance.DisableGoalkickBoundaries();
        ServerManager.instance.SetAllDetectorsToInactive();
        ServerManager.instance.ResetOutOfBoundsTimer();
    }

    [ServerRpc(RequireOwnership = false)]
    public void EndOutOfBoundsPlayServerRpc(float delay)
    {
        isBallPickedUp.Value = false;
        Invoke(nameof(EndOutOfBoundsPlay), delay);
    }

    private void EndOutOfBoundsPlay()
    {
        isOutOfBounds.Value = false;
        isThrowIn.Value = false;
        isCornerKick.Value = false;
        isGoalKick.Value = false;

        ServerManager.instance.SetAllDetectorsToInactive();
        ServerManager.instance.ResetOutOfBoundsTimer();
    }

    #endregion
}