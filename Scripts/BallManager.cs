using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

public class BallManager : NetworkBehaviour
{
    public static BallManager instance;

    [Header("Prefab")]
    [SerializeField] private NetworkObject ballPrefab;

    [Header("Main Ball")]
    public GameObject mainBallObj;
    public BallSync mainBallSync;

    public List<BallSync> ActiveBalls = new List<BallSync>();
    private int _ballCounter = 0;

    // tracks which clients spawned which ball to prevent duplications
    private Dictionary<ulong, NetworkObject> _clientSpawnedBalls = new Dictionary<ulong, NetworkObject>();

    private void Awake()
    {
        if (instance != null && instance != this)
            Destroy(gameObject);
        else
            instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        ActiveBalls.Add(mainBallSync);
        _ballCounter++;
    }

    private void Start()
    {
        if (!IsServer)
            return;

        if (ServerManager.instance.isTutorialServer)
            mainBallObj.SetActive(false);
    }

    public void SpawnNonPlayerOwnedBall(Vector3 position)
    {
        GameObject ballInstance = Instantiate(ballPrefab.gameObject, position, Quaternion.identity);
        NetworkObject networkObject = ballInstance.GetComponent<NetworkObject>();

        networkObject.Spawn();

        // set the position again
        networkObject.GetComponentInChildren<Rigidbody>().position = position;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestBallSpawnServerRpc(Vector3 position, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        // if the client already has a ball spawned
        // do nothing
        if (_clientSpawnedBalls.ContainsKey(clientId))
            return;

        GameObject ballInstance = Instantiate(ballPrefab.gameObject, position, Quaternion.identity);
        NetworkObject networkObject = ballInstance.GetComponent<NetworkObject>();

        networkObject.Spawn();

        // set the position again
        networkObject.GetComponentInChildren<Rigidbody>().position = position;

        // track the id
        _clientSpawnedBalls[clientId] = networkObject;

        // send message back to the client to assign its new local ball
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        };

        AssignSpawnedBallClientRpc(networkObject.NetworkObjectId, clientRpcParams);
    }

    [ClientRpc]
    private void AssignSpawnedBallClientRpc(ulong ballNetworkObjectId, ClientRpcParams clientRpcParams = default)
    {
        // find the newly spawned ball in the local scene using its ID
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(ballNetworkObjectId, out NetworkObject ballNetworkObject))
            // spoawn the ball
            HandleKicking.instance.SetLocalSpawnedBall(ballNetworkObject.gameObject);
    }

    public void RegisterBall(BallSync ball)
    {
        if (!ActiveBalls.Contains(ball))
            ActiveBalls.Add(ball);
    }

    public void UnregisterBall(BallSync ball)
    {
        if (ActiveBalls.Contains(ball))
            ActiveBalls.Remove(ball);
    }

    public BallSync GetLocalSpawnedBall(ulong clientId)
    {
        return _clientSpawnedBalls[clientId].GetComponentInChildren<BallSync>();
    }

    public BallSync FindNearestBall(Vector3 position)
    {
        BallSync nearestBall = null;
        float minDistanceSquared = float.MaxValue;

        if (ActiveBalls.Count == 0)
            return null;

        foreach (BallSync ball in ActiveBalls)
        {
            if (ball == null) 
                continue;

            float distanceSquared = (ball.transform.position - position).sqrMagnitude;

            if (distanceSquared < minDistanceSquared)
            {
                minDistanceSquared = distanceSquared;
                nearestBall = ball;
            }
        }

        return nearestBall;
    }

    public void ClearAllClientBalls()
    {
        _clientSpawnedBalls.Clear();
    }

    public void ResetAllBalls(Vector3 position)
    {
        if (!IsServer) return;

        foreach (var ball in ActiveBalls)
        {
            ball.ResetBallServerRpc(position);
        }
    }

    public void DeleteNonMainBalls()
    {
        if (!IsServer) 
            return;

        List<BallSync> ballsToDespawn = ActiveBalls.Where(b => b != mainBallSync).ToList();

        if (ServerManager.instance.isTutorialServer)
        {
            HandleKicking.instance.DeleteLocalSpawnedBall();
            _clientSpawnedBalls.Remove(NetworkManager.LocalClientId);
        }

        foreach (var ball in ballsToDespawn)
            if (ball != null && ball.NetworkObject != null && ball.NetworkObject.IsSpawned)
                ball.NetworkObject.Despawn(true);
    }
}

