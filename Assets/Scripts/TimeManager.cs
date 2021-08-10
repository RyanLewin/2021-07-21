using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;
    private LewinNetworkManager LewinNetworkManager;
    [SerializeField] private float timer;
    public float timePerTurn = 5f;
    [SerializeField] private int playerTurn = 0;
    private bool gameStarted;

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
        if (gameStarted)
        {
            timer += Time.deltaTime;
            if (timer >= timePerTurn)
            {
                timer = 0;
                SetNextPlayerServerRpc();
            }
        }
    }

    private void TimerServerRpc()
    {
        
    }

    [ServerRpc]
    public void SetGameStartedServerRpc()
    {
        gameStarted = true;
        LewinNetworkManager.ConnectedPlayerList[0].SetCanPlayerMoveClientRpc(true);
    }

    [ServerRpc]
    private void SetNextPlayerServerRpc()
    {
        LewinNetworkManager.ConnectedPlayerList[playerTurn].SetCanPlayerMoveClientRpc(false);
        playerTurn++;
        if (playerTurn >= LewinNetworkManager.ConnectedPlayerList.Count)
            playerTurn = 0;
        LewinNetworkManager.ConnectedPlayerList[playerTurn].SetCanPlayerMoveClientRpc(true);
        print(LewinNetworkManager.ConnectedPlayerList[playerTurn].Name.Value);
    }
}
