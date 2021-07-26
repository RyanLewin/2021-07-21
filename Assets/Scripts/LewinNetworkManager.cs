using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class LewinNetworkManager : MonoBehaviour
{
    public static LewinNetworkManager Instance;
    ConsoleLogger ConsoleLogger => ConsoleLogger.Instance;
    NetworkManager NetworkManager => NetworkManager.Singleton;
    [SerializeField] GameObject objMenuUI;
    [SerializeField] GameObject objPlayUI;

    [SerializeField] Dictionary<ulong, string> connectedPlayers = new Dictionary<ulong, string>();
    // [SerializeField] private GameObject btnHost;
    // [SerializeField] private GameObject btnJoin;
    // [SerializeField] private GameObject btnServer;

    private void OnGUI() 
    {
        var mode = NetworkManager.IsHost ?
            "Host" : NetworkManager.IsServer ? "Server" : "Client";

        GUILayout.Label("Mode: " + mode);
    }

    private void Awake() 
    {
        Instance = this;
    }

    private void Start() {
        // NetworkManager.OnClientDisconnectCallback += OnDisconnect;
        // NetworkManager.OnClientConnectedCallback += OnConnect;
    }

    public void HostGame() => NetworkManager.StartHost();
    public void JoinGame() => NetworkManager.StartClient();
    public void HostServer() => NetworkManager.StartServer();

    public void OnConnect(ulong id)
    {
        if (NetworkManager.ConnectedClients.TryGetValue(id, out var Client))
        {
            var player = Client.PlayerObject.GetComponent<PlayerController>();
            print($"{player.Name.Value} Connected");
        }
    }

    public void Disconnected(ulong playerID){
        OnDisconnect(playerID);
    }

    private void OnDisconnect(ulong playerID) {
        // if (NetworkManager.ConnectedClients.TryGetValue(playerID, out var Client))
        // {
        //     var player = Client.PlayerObject.GetComponent<PlayerController>();
        //     print($"{player.Name.Value} Disconnected");
        // }

        objMenuUI.SetActive(true);
        objPlayUI.SetActive(false);
    }

    public void HideMenuUI()
    {
        objMenuUI.SetActive(false);
        objPlayUI.SetActive(true);
        // btnHost.SetActive(false);
        // btnJoin.SetActive(false);
        // btnServer.SetActive(false);
    }
}
