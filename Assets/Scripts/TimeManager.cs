using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;

public class TimeManager : NetworkBehaviour
{
    public static TimeManager Instance;
    private LewinNetworkManager LewinNetworkManager;
    [SerializeField] private float timer;
    public float timePerTurn = 5f;
    [SerializeField] private int playerTurn = 0;
    private bool gameStarted;
    public bool IsGamePaused { get; private set; }
    private NetworkVariableBool GamePaused = new NetworkVariableBool(new NetworkVariableSettings{
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
    }

    private void FixedUpdate()
    {
        IsGamePaused = GamePaused.Value;
        if (gameStarted && !GamePaused.Value)
        {
            timer += Time.deltaTime;
            if (timer >= timePerTurn)
            {
                SetNextPlayerServerRpc();
            }
        }
    }

    public void SetGameStarted()
    {
        gameStarted = true;
        LewinNetworkManager.ConnectedPlayerList[0].SetCanPlayerMoveClientRpc(true);
    }

    public void SetGamePaused(bool value, bool resetTimer = true)
    {
        GamePaused.Value = value;
        if (resetTimer)
            timer = 0;
    }

    [ServerRpc]
    public void SetNextPlayerServerRpc()
    {
        timer = 0;
        LewinNetworkManager.ConnectedPlayerList[playerTurn].SetCanPlayerMoveClientRpc(false);
        playerTurn++;
        if (playerTurn >= LewinNetworkManager.ConnectedPlayerList.Count)
            playerTurn = 0;
        LewinNetworkManager.ConnectedPlayerList[playerTurn].SetCanPlayerMoveClientRpc(true);
        // print(LewinNetworkManager.ConnectedPlayerList[playerTurn].Name.Value);
    }
}
