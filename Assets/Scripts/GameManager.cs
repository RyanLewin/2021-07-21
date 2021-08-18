using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public NetworkVariableInt ConnectedPlayerCount = new NetworkVariableInt(new NetworkVariableSettings
    {
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });

    private void Awake()
    {
        Instance = this;
    }

    public void SetConnectedPlayerCount(int count)
    {
        ConnectedPlayerCount.Value = count;
    }
}
