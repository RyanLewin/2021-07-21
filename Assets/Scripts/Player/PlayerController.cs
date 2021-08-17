using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using UnityEngine.Networking;
using System.Collections;
using PropertyListenerTool;

public class PlayerController : NetworkBehaviour
{
    UIManager UIManager => UIManager.Instance;
    [SerializeField] GameObject UICanvasPrefab;
    LewinNetworkManager LewinNetworkManager => LewinNetworkManager.Instance;
    PlayerConsoleManager PlayerConsoleManager;
    public PlayerInput playerInput { get; private set; }
    public bool isPlayersTurn;
    public NetworkVariableBool CanPlayerMove = new NetworkVariableBool(new NetworkVariableSettings
    {
        WritePermission = NetworkVariablePermission.OwnerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });
    Camera playerCamera;
    [SerializeField] Unit unitToSpawn;
    [SerializeField] Unit selectedUnit;
    public NetworkVariableBool HasUnitSelected = new NetworkVariableBool(new NetworkVariableSettings
    {
        WritePermission = NetworkVariablePermission.OwnerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });
    public int TeamNumber { get; private set; }
    [SerializeField] Transform birdsEyeViewCamPosition;
    Transform spawnPoint;
    Vector3 initialPosition;
    Vector3 relativeCamPosition;
    Vector3 initialRotation;
    Vector3 relativeCamRotation;
    [SerializeField] Vector3 offsetToUnit;

    public NetworkVariableBool UnitDied = new NetworkVariableBool(true);
    public NetworkVariableVector3 Position = new NetworkVariableVector3(new NetworkVariableSettings
    {
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });

    public NetworkVariableString Name = new NetworkVariableString(new NetworkVariableSettings
    {
        WritePermission = NetworkVariablePermission.OwnerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });

    // string m;
    // private void OnGUI() {
    //     GUILayout.Space(100);
    //     GUILayout.Label(NetworkManager.Singleton.ConnectedClients.Count.ToString());
    // }

    public override void NetworkStart()
    {
        if (!IsLocalPlayer) return;

        gameObject.name = $"{gameObject.name}_{NetworkManager.LocalClientId}";
        playerInput = new PlayerInput();
        EnableInput();
        PlayerConsoleManager = GetComponent<PlayerConsoleManager>();
        PlayerConsoleManager.SetPlayerInput(playerInput);

        UIManager.btnDeselect.onClick.AddListener(DeselectUnit);
        UIManager.btnDisconnect.onClick.AddListener(DisconnectPlayer);
        UIManager.btnGameEndDisconnect.onClick.AddListener(DisconnectPlayer);
        UIManager.btnGameEndDisconnect.onClick.AddListener(() => UIManager.gameEndUI.SetActive(false));
        UIManager.btnChoose.onClick.AddListener(ChooseUnit);
        UIManager.btnChoose.interactable = false;

        var playerName = UIManager.inputPlayerName.text;
        if (NetworkManager.IsServer)
            Name.Value = playerName;
        else
            SubmitNameChangeServerRpc(playerName);
        PlayerConsoleManager.SetPlayerName(playerName);
        PlayerConsoleManager.LogMessage($"{playerName} has joined.", "Server");
        ConnectedPlayerServerRpc(OwnerClientId);

        var spawnPoints = GameObject.FindGameObjectWithTag("SpawnPoints").transform;
        var playerSpawn = new Transform[spawnPoints.childCount];
        for (int i = 0; i < spawnPoints.childCount; i++)
        {
            playerSpawn[i] = spawnPoints.GetChild(i);
        }

        TeamNumber = GameManager.Instance.ConnectedPlayerCount.Value % 2;
        var childNumber = GameManager.Instance.ConnectedPlayerCount.Value == 0 ? 0 : 1;
        spawnPoint = playerSpawn[childNumber];
        var playerSpawnPoint = spawnPoint.GetChild(0);

        birdsEyeViewCamPosition = GameObject.FindGameObjectWithTag("BirdsCameraPoints").transform.GetChild(childNumber);

        playerCamera = Camera.main;
        initialPosition = playerSpawnPoint.position;
        initialRotation = playerSpawnPoint.eulerAngles;
        playerCamera.transform.position = initialPosition;
        playerCamera.transform.eulerAngles = initialRotation;

        SpawnUnitServerRpc(spawnPoint.position, spawnPoint.rotation, NetworkManager.LocalClientId);
    }

    private void OnEnable()
    {
        if (!IsLocalPlayer) return;
        if (playerInput != null)
            EnableInput();
    }

    private void EnableInput()
    {
        if (playerInput == null) return;
        playerInput.KeyboardMouse.MouseClick.started += SelectUnit;
        playerInput.Enable();
    }

    private void OnDisable()
    {
        if (!IsLocalPlayer) return;
        if (playerInput == null) return;
        playerInput.KeyboardMouse.MouseClick.started -= SelectUnit;
        playerInput.Disable();
    }

#if !UNITY_EDITOR
    private void OnDestroy() 
    {
        print($"{Name.Value} Disconnected");
    }
#endif

    private void OnApplicationQuit()
    {
        print($"{Name.Value} Disconnected");
    }

    [ServerRpc]
    private void SubmitNameChangeServerRpc(string playerName)
    {
        Name.Value = playerName;
    }

    [ServerRpc]
    private void ConnectedPlayerServerRpc(ulong id)
    {
        LewinNetworkManager.OnConnect(id, this);
    }

    public void DisconnectPlayer()
    {
        // playerCamera.transform.parent = null;
        // Cursor.lockState = CursorLockMode.None;
        // Instantiate(UICanvasPrefab);

        if (LewinNetworkManager)
        {
            LewinNetworkManager.Disconnected();
            if (IsHost)
                LewinNetworkManager.OnDisconnect(OwnerClientId);
            Destroy(NetworkManager.Singleton.gameObject);
        }
        NetworkManager.StopClient();
        if (IsHost)
            NetworkManager.StopHost();
        if (IsServer)
            NetworkManager.StopServer();

        UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
    }

    private void SelectUnit(InputAction.CallbackContext context)
    {
        if (context.started && !selectedUnit)
        {
            Vector3 mouseScreenPos = playerInput.KeyboardMouse.PointerPosition.ReadValue<Vector2>();
            var mousePos = playerCamera.ScreenPointToRay(mouseScreenPos);
            if (Physics.Raycast(mousePos, out RaycastHit hit, 1000f))
            {
                if (hit.transform.CompareTag("Unit"))
                {
                    if (hit.transform.TryGetComponent<Unit>(out selectedUnit))
                    {
                        if (!hit.transform.GetComponent<NetworkObject>().IsOwner)
                            return;
                        // pointerPos = hit.point;
                        UIManager.btnChoose.interactable = true;
                        LeanTween.rotateLocal(playerCamera.gameObject, selectedUnit.transform.eulerAngles, .2f);
                        LeanTween.move(playerCamera.gameObject, selectedUnit.transform.position + offsetToUnit, .2f).setOnComplete(() =>
                        {
                            playerCamera.transform.parent = selectedUnit.transform;
                        });
                    }
                }
            }
        }
    }

    private void ChooseUnit()
    {
        if (!selectedUnit) return;

        selectedUnit.Initialise(this, playerInput, playerCamera, NetworkManager.LocalClientId);
        selectedUnit.SetToControl();
        HasUnitSelected.Value = true;

        if (IsHost)
        {
            UIManager.btnDisconnect.onClick.RemoveAllListeners();
            UIManager.btnDisconnect.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Play";
            UIManager.btnDisconnect.onClick.AddListener(SetGameStarted);
        }
        else
        {
            UIManager.btnDisconnect.gameObject.SetActive(false);
        }
        // SetGamePausedServerRpc(false);
    }

    private void SetGameStarted()
    {
        foreach (var player in LewinNetworkManager.ConnectedPlayerList)
        {
            if (!player.HasUnitSelected.Value)
            {
                PlayerConsoleManager.LogMessage($"{player.Name.Value} isn't ready yet", "Server", OwnerClientId);
                return;
            }
        }
        TimeManager.Instance.SetGameStarted();
        Cursor.lockState = CursorLockMode.Locked;
        UIManager.btnDisconnect.gameObject.SetActive(false);
    }

    [ServerRpc]
    public void SetGamePausedServerRpc(bool value)
    {
        TimeManager.Instance.SetGamePaused(value);
    }

    public void SpawnNewUnit()
    {
        selectedUnit = null;
        HasUnitSelected.Value = false;
        LeanTween.move(playerCamera.gameObject, initialPosition, .2f);
        LeanTween.rotate(playerCamera.gameObject, initialRotation, .2f);
        SpawnUnitServerRpc(spawnPoint.position, spawnPoint.rotation, NetworkManager.LocalClientId);
    }

    [ServerRpc]
    private void SpawnUnitServerRpc(Vector3 spawnPoint, Quaternion rotation, ulong localID)
    {
        var unit = Instantiate(unitToSpawn, spawnPoint, rotation);
        unit.GetComponent<NetworkObject>().SpawnWithOwnership(localID);
    }

    private void DeselectUnit()
    {
        if (!selectedUnit) return;

        selectedUnit.ReleaseControl();
        UIManager.btnChoose.interactable = false;
        selectedUnit = null;
        HasUnitSelected.Value = false;
        playerCamera.transform.parent = null;
        LeanTween.move(playerCamera.gameObject, initialPosition, .2f);
        LeanTween.rotate(playerCamera.gameObject, initialRotation, .2f);
    }

    [ClientRpc]
    public void SetPlayersTurnClientRpc(bool value)
    {
        if (selectedUnit)
            selectedUnit.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        isPlayersTurn = value;
    }

    [ClientRpc]
    public void SetCamPositionClientRpc(bool playersTurn, ClientRpcParams clientRpcParams = default)
    {
        if (playersTurn && selectedUnit)
        {
            LeanTween.rotateLocal(playerCamera.gameObject, Vector3.zero, .2f);
            LeanTween.move(playerCamera.gameObject, selectedUnit.transform.position + offsetToUnit, .2f);/* .setOnComplete(() => {
                playerCamera.transform.parent = selectedUnit.transform;
            }); */
        }
        else if (!selectedUnit)
        {
            LeanTween.rotate(playerCamera.gameObject, initialRotation, .2f);
            LeanTween.move(playerCamera.gameObject, initialPosition, .2f);
        }
        else
        {
            // playerCamera.transform.parent = null;
            LeanTween.rotate(playerCamera.gameObject, birdsEyeViewCamPosition.eulerAngles, .2f);
            LeanTween.move(playerCamera.gameObject, birdsEyeViewCamPosition.position, .2f);
        }
    }
}
