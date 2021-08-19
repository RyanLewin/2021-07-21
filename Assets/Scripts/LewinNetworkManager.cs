using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;

public class LewinNetworkManager : MonoBehaviour
{
    public static LewinNetworkManager Instance;
    ConsoleLogger ConsoleLogger => ConsoleLogger.Instance;
    NetworkManager NetworkManager => NetworkManager.Singleton;

    public Dictionary<ulong, PlayerController> ConnectedPlayers;
    public List<PlayerController> ConnectedPlayerList { get; private set; }
    // [SerializeField] private GameObject btnHost;
    // [SerializeField] private GameObject btnJoin;
    // [SerializeField] private GameObject btnServer;

    private void OnGUI()
    {
        var mode = NetworkManager.IsHost ?
            "Host" : NetworkManager.IsServer ? "Server" : "Client";

        GUILayout.Label("Mode: " + mode);
        GUILayout.Label(NetworkManager.LocalClientId.ToString());
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

    private void OnDestroy()
    {
        if (!NetworkManager)
            return;
        NetworkManager.OnClientDisconnectCallback -= OnDisconnect;
    }

    public void HostGame() => NetworkManager.StartHost();
    public void JoinGame() => NetworkManager.StartClient();
    public void HostServer() => NetworkManager.StartServer();

    [ServerRpc]
    public void OnConnectServerRpc(ulong playerID, PlayerController player)
    {
        ConnectedPlayers.Add(playerID, player);
        ConnectedPlayerList.Add(player);
        if (NetworkManager.IsHost || NetworkManager.IsServer)
            GameManager.Instance.SetConnectedPlayerCount(ConnectedPlayerList.Count);
    }

    public void OnDisconnect(ulong playerID)
    {
        if (ConnectedPlayerList.Count == 0) return;

        if (PlayerConsoleManager.Instance)
            PlayerConsoleManager.Instance.LogMessageServerRpc($"{ConnectedPlayers[playerID].Name.Value} has Left.", "Server");

        ConnectedPlayerList.Remove(ConnectedPlayers[playerID]);
        ConnectedPlayers.Remove(playerID);

        if (NetworkManager.IsHost || NetworkManager.IsServer)
        {
            GameManager.Instance.SetConnectedPlayerCount(ConnectedPlayerList.Count);
            if (NetworkManager.IsServer && ConnectedPlayerList.Count == 0)
            {
                NetworkManager.StopServer();
                UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
            }
        }
    }

    public void HideMenuUI()
    {
        UIManager.Instance.SetUI(UIWindows.UnitSelection);
    }
}
