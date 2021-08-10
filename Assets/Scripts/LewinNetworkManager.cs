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

    public Dictionary<ulong, PlayerController> ConnectedPlayers {get; private set;}
    public List<PlayerController> ConnectedPlayerList {get; private set;}
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
        ConnectedPlayers = new Dictionary<ulong, PlayerController>();
        ConnectedPlayerList = new List<PlayerController>();
        Instance = this;
    }

    private void Start()
    {
        NetworkManager.OnClientDisconnectCallback += OnDisconnect;
    }

    public void HostGame() => NetworkManager.StartHost();
    public void JoinGame() => NetworkManager.StartClient();
    public void HostServer() => NetworkManager.StartServer();

    public void OnConnect(ulong playerID, PlayerController player)
    {
        // foreach(var host in connectedPlayers)
        //     PlayerConsoleManager.Instance.LogMessage($"Playing against {host.Value}.", playerID);
        ConnectedPlayers.Add(playerID, player);
        ConnectedPlayerList.Add(player);
    }

    public void Disconnected(ulong playerID)
    {
        objMenuUI.SetActive(true);
        objPlayUI.SetActive(false);
    }

    public void OnDisconnect(ulong playerID)
    {
        if (PlayerConsoleManager.Instance)
            PlayerConsoleManager.Instance.LogMessage($"{ConnectedPlayers[playerID].Name.Value} has Left.");
        ConnectedPlayerList.Remove(ConnectedPlayers[playerID]);
        ConnectedPlayers.Remove(playerID);
    }

    public void HideMenuUI()
    {
        objMenuUI.SetActive(false);
        objPlayUI.SetActive(true);
    }
}
