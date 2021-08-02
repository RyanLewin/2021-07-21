using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using UnityEngine.Networking;

public class PlayerController : NetworkBehaviour
{
    UIManager UIManager => UIManager.Instance;
    LewinNetworkManager LewinNetworkManager => LewinNetworkManager.Instance;
    ConsoleLogger ConsoleLogger => ConsoleLogger.Instance;
    public PlayerInput playerInput {get; private set;}

    Camera playerCamera;
    // Transform[] playerSpawn;
    [SerializeField] Unit unitToSpawn;
    [SerializeField] Unit selectedUnit;
    Vector3 pointerPos;

    
    [SerializeField] float moveSpeed; 
    Vector3 initialPosition;
    Vector3 initialRotation;
    [SerializeField] Vector3 offsetToUnit;
    [SerializeField] Vector3 rotationToUnit;

    public NetworkVariableVector3 Position = new NetworkVariableVector3(new NetworkVariableSettings {
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });

    public NetworkVariableString Name = new NetworkVariableString(new NetworkVariableSettings {
        WritePermission = NetworkVariablePermission.OwnerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });

    // Start is called before the first frame update
    public override void NetworkStart()
    {
        if (!IsLocalPlayer) return;

        playerCamera = Camera.main;
        initialPosition = playerCamera.transform.position;
        initialRotation = playerCamera.transform.eulerAngles;

        playerInput = new PlayerInput();
        EnableInput();
        GetComponent<PlayerConsoleManager>().SetPlayerInput(playerInput);

        UIManager.btnDeselect.onClick.AddListener(DeselectUnit);
        UIManager.btnDisconnect.onClick.AddListener(DisconnectPlayer);
        UIManager.btnChoose.onClick.AddListener(ChooseUnit);

        var playerName = UIManager.inputPlayerName.text;
        if (NetworkManager.IsServer)
            Name.Value = playerName;
        else
            SubmitNameChangeServerRpc(playerName);
        ConnectedPlayerServerRpc(NetworkManager.LocalClientId);

        // print(OwnerClientId + " " + NetworkManager.LocalClientId);
        SpawnUnitServerRpc(OwnerClientId, NetworkManager.LocalClientId);

        if (OwnerClientId > 0)
        {
            var camPos = Camera.main.transform.position;
            camPos.z += 12.85f;
            Camera.main.transform.position = camPos;
            Camera.main.transform.eulerAngles = new Vector3(20, 180, 0);
        }
    }

    private void OnEnable() 
    {
        if (!IsLocalPlayer) return;
        if (playerInput != null)
            EnableInput();
    }

    private void EnableInput() 
    {
        if (playerInput == null ) return;
        playerInput.KeyboardMouse.MouseClick.started += SelectUnit;
        playerInput.Enable();
    }

    private void OnDisable() {
        if (!IsLocalPlayer) return;
        if (playerInput == null) return;
        playerInput.KeyboardMouse.MouseClick.started -= SelectUnit;
        playerInput.Disable();
    }

#if !UNITY_EDITOR
    private void OnDestroy() 
    {
        print($"{Name.Value} Disconnected");
        // if (!IsLocalPlayer)
        //     DisconnectPlayer();
    }
#endif

    private void OnApplicationQuit() 
    {
        print($"{Name.Value} Disconnected");
        // if (!IsLocalPlayer)
        //     DisconnectPlayer();
    }

    [ServerRpc]
    private void SubmitNameChangeServerRpc(string playerName)
    {
        Name.Value = playerName;
    }

    [ServerRpc]
    private void ConnectedPlayerServerRpc(ulong id)
    {
        LewinNetworkManager.OnConnect(id);
    }

    private void DisconnectPlayer()
    {
        // ConsoleLogger.AddMessageServerRpc("Hello Holaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        if (LewinNetworkManager)
            LewinNetworkManager.Disconnected(NetworkManager.LocalClientId);
        NetworkManager.StopClient();
        if (IsHost)
            NetworkManager.StopHost();
        if (IsServer)
            NetworkManager.StopServer();
    }

    private bool CanControlPlayer { get => (Application.isFocused && IsLocalPlayer); }

    private void Movement()
    {
        if (!CanControlPlayer) return;

        Vector3 moveDirection = playerInput.KeyboardMouse.Move.ReadValue<Vector2>();
        if (moveDirection != Vector3.zero)
        {
            moveDirection.z = moveDirection.y;
            moveDirection.y = 0;
            SubmitPositionRequestServerRpc(transform.position - moveDirection * moveSpeed * Time.deltaTime);
        }
    }

    private void SelectUnit(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Vector3 mouseScreenPos = playerInput.KeyboardMouse.PointerPosition.ReadValue<Vector2>();
            var mousePos = playerCamera.ScreenPointToRay(mouseScreenPos);
            if (Physics.Raycast(mousePos, out RaycastHit hit, 1000f/* , LayerMask.NameToLayer("Unit") */))
            {
                if (hit.transform.CompareTag("Unit"))
                {
                    if (hit.transform.TryGetComponent<Unit>(out selectedUnit))
                    {
                        // pointerPos = hit.point;
                        UIManager.btnChoose.interactable = true;
                        LeanTween.rotateLocal(playerCamera.gameObject, selectedUnit.transform.eulerAngles, .2f);
                        LeanTween.move(playerCamera.gameObject, selectedUnit.transform.position + offsetToUnit, .2f).setOnComplete(() => {
                            playerCamera.transform.parent = selectedUnit.transform;
                        });
                    }
                }
            }
        }
    }

    private void ChooseUnit()
    {
        
        // selectedUnit = Instantiate(unitToSpawn, Vector3.zero + Vector3.up * 2, Quaternion.identity);
        // selectedUnit.GetComponent<NetworkObject>().SpawnWithOwnership(NetworkManager.LocalClientId);
        selectedUnit.Initialise(this, playerInput, playerCamera, NetworkManager.LocalClientId);
        selectedUnit.SetToControl();
    }

    [ServerRpc]
    private void SpawnUnitServerRpc(ulong ownerID, ulong localID)
    {
        var spawnPoints = GameObject.FindGameObjectWithTag("SpawnPoints").transform;
        var playerSpawn = new Transform[spawnPoints.childCount];
        for(int i = 0; i < spawnPoints.childCount; i++)
        {
            playerSpawn[i] = spawnPoints.GetChild(i);
        }
        selectedUnit = Instantiate(unitToSpawn, playerSpawn[ownerID == 0 ? 0 : 1].position, Quaternion.Euler(ownerID == 0 ? Vector3.zero : new Vector3(0,180,0)));
        selectedUnit.GetComponent<NetworkObject>().SpawnWithOwnership(localID);
    }

    private void DeselectUnit()
    {
        selectedUnit.ReleaseControl();
        UIManager.btnChoose.interactable = false;
        selectedUnit = null;
        playerCamera.transform.parent = null;
        LeanTween.move(playerCamera.gameObject, initialPosition, .2f);
        LeanTween.rotate(playerCamera.gameObject, initialRotation, .2f);
    }

    [ServerRpc]
    private void SubmitPositionRequestServerRpc(Vector3 newPos, ServerRpcParams rpcParams = default)
    {
        Position.Value = newPos;
    }

    void FixedUpdate()
    {
        // transform.position = Vector3.Lerp(transform.position, transform.position - Vector3.right, Time.deltaTime);
        transform.position = Position.Value;
    }
}
