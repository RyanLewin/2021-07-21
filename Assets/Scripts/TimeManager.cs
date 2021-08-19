using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;

public class TimeManager : NetworkBehaviour
{
    public static TimeManager Instance;
    UIManager UIManager;
    private LewinNetworkManager LewinNetworkManager;
    private NetworkVariableFloat timer = new NetworkVariableFloat(new NetworkVariableSettings
    {
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });
    public float timePerTurn = 5f;
    [SerializeField] private int playerTurn = 0;
    public NetworkVariableBool GameStarted;
    // public bool IsGameStarted { get { return GameStarted; } }
    public bool IsGamePaused { get; private set; }
    private NetworkVariableBool GamePaused = new NetworkVariableBool(new NetworkVariableSettings
    {
        WritePermission = NetworkVariablePermission.Everyone,
        ReadPermission = NetworkVariablePermission.Everyone
    });
    private NetworkVariableBool GameEnded = new NetworkVariableBool(new NetworkVariableSettings
    {
        WritePermission = NetworkVariablePermission.Everyone,
        ReadPermission = NetworkVariablePermission.Everyone
    });

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        LewinNetworkManager = LewinNetworkManager.Instance;
        UIManager = UIManager.Instance;
    }

    private void FixedUpdate()
    {
        IsGamePaused = GamePaused.Value;
        if (GameStarted.Value && !GamePaused.Value && (IsHost || IsServer))
        {
            // print(gameStarted + " - " + !GamePaused.Value + " - " + timer.Value);
            timer.Value += Time.deltaTime;
            // IncreaseTimerServerRpc(Time.deltaTime);
            if (timer.Value >= timePerTurn)
            {
                SetNextPlayer();
            }
            UpdateTimerClientRpc(timePerTurn - timer.Value);
        }
    }

    [ClientRpc]
    private void UpdateTimerClientRpc(float time)
    {
        time = Mathf.Clamp(time, 0, time);
        UIManager.timerText.text = $"Timer: {time.ToString("0.00")}";
    }

    [ServerRpc]
    public void ResetTimerServerRpc()
    {
        timer.Value = 0;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetGameStartedServerRpc()
    {
        GameStarted.Value = true;
        GameEnded.Value = false;
        LewinNetworkManager.ConnectedPlayerList[0].SetPlayersTurnClientRpc(true);
    }

    public void SetGamePaused(bool value, bool resetTimer = true)
    {
        if (GameEnded.Value)
            return;
        GamePaused.Value = value;
        if (resetTimer)
            ResetTimerServerRpc();
    }

    public void SetGameEnded()
    {
        SetGamePaused(true);
        GameEnded.Value = true;
    }

    public void SetNextPlayer()
    {
        timer.Value = 0;
        if (LewinNetworkManager.ConnectedPlayerList.Count == 1) return;
        
        var oldPlayer = LewinNetworkManager.ConnectedPlayerList[playerTurn];
        ClientRpcParams clientRpcSendParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { oldPlayer.OwnerClientId }
            }
        };
        oldPlayer.SetPlayersTurnClientRpc(false);
        oldPlayer.SetCamPositionClientRpc(false, clientRpcSendParams);
        playerTurn++;
        if (playerTurn >= LewinNetworkManager.ConnectedPlayerList.Count)
            playerTurn = 0;
        var newPlayer = LewinNetworkManager.ConnectedPlayerList[playerTurn];
        clientRpcSendParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { newPlayer.OwnerClientId }
            }
        };
        newPlayer.SetPlayersTurnClientRpc(true);
        newPlayer.SetCamPositionClientRpc(true, clientRpcSendParams);
        SetGamePaused(true);
        // print(LewinNetworkManager.ConnectedPlayerList[playerTurn].Name.Value);
    }
}
