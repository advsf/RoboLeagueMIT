using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class HandleConnections : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject hostDisconnectionUI;

    [Header("Session Management")]
    [SerializeField] private SessionHolder sessionHolder;

    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 1f;
    [SerializeField] private float transitionDelay = 2f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;

        if (hostDisconnectionUI != null)
            hostDisconnectionUI.SetActive(false);
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;

        base.OnNetworkDespawn();
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        // handle when the host leaves
        if (clientId == NetworkManager.ServerClientId || clientId == NetworkManager.Singleton.LocalClientId)
        {
            if (!NetworkManager.IsHost)
                hostDisconnectionUI.SetActive(true);

            if (clientId == NetworkManager.LocalClientId)
                NetworkManager.Singleton.Shutdown();

            Invoke(nameof(ReturnToLobby), transitionDelay);
        }
    }

    // called via a button
    public void LeaveGame() => ReturnToLobby();

    private async void ReturnToLobby()
    {
        await sessionHolder.ActiveSession.LeaveAsync();

        HandleTransitions.instance.PlayFadeInTransition();
        HandleTransitions.instance.PlayFadeInMusic();

        // wait for the transition to finish
        await Task.Delay((int)(transitionDuration * 1000));

        // load the scene
        SceneManager.LoadScene("Lobby");
    }
}