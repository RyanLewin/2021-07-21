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

    private void Start() 
    {
        NetworkManager.OnClientDisconnectCallback += OnDisconnect;
    }

    public void HostGame() => NetworkManager.StartHost();
    public void JoinGame() => NetworkManager.StartClient();
    public void HostServer() => NetworkManager.StartServer();

    public void OnConnect(ulong playerID, string playerName)
    {
        // foreach(var host in connectedPlayers)
        //     PlayerConsoleManager.Instance.LogMessage($"Playing against {host.Value}.", playerID);
        connectedPlayers.Add(playerID, playerName);
    }

    public void Disconnected(ulong playerID){
        objMenuUI.SetActive(true);
        objPlayUI.SetActive(false);
    }

    public void OnDisconnect(ulong playerID) {
        PlayerConsoleManager.Instance.LogMessage($"{connectedPlayers[playerID]} has Left.");
        connectedPlayers.Remove(playerID);
    }

    public void HideMenuUI()
    {
        objMenuUI.SetActive(false);
        objPlayUI.SetActive(true);
    }
}
