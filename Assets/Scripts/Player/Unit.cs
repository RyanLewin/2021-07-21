using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using UnityEngine.InputSystem;

public class Unit : NetworkBehaviour
{
    PlayerController playerController;
    PlayerConsoleManager playerConsoleManager;
    TimeManager TimeManager;
    PlayerInput playerInput;
    Rigidbody rigidBody;
    public bool CanControl { get; set; }
    public bool isGrounded { get; private set; }
    [SerializeField] Transform groundCheckPos;
    [SerializeField] LayerMask groundMask;
    [SerializeField] float moveSpeed;
    Camera playerCamera;
    [SerializeField] float gravityScale;
    float xRotation = 0f;
    [SerializeField] float horizontalRotateSpeed = 10f;
    Vector3 newRotation;
    [SerializeField] float verticalRotateSpeed = 10f;
    [SerializeField] bool invertY = false;
    [SerializeField] float jumpForce = 5f;
    [SerializeField] float stepHeight;
    Transform[] stepChecks;

    [Space(10)]
    [SerializeField] bool swapPlayerOnShoot;
    public NetworkVariableFloat Health = new NetworkVariableFloat(100);
    bool isDead = false;
    [SerializeField] LayerMask bulletMask;

    bool scope = false;

    public NetworkVariableVector3 Position = new NetworkVariableVector3(new NetworkVariableSettings
    {
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });

    // private void OnGUI()
    // {
    //     if (!IsOwner) return;
    //     GUILayout.Space(50);
    //     if (playerController)
    //         GUILayout.Label(playerController.isPlayersTurn.ToString());
    //     GUILayout.Label(rigidBody.angularVelocity.ToString());
    // }

    private void Awake()
    {
        newRotation = transform.eulerAngles;
        rigidBody = GetComponent<Rigidbody>();

        stepChecks = new Transform[4];
        for (int i = 0; i < 4; ++i)
        {
            stepChecks[i] = groundCheckPos.GetChild(i);
            stepChecks[i].transform.localPosition += Vector3.up * stepHeight;
        }
    }

    public override void NetworkStart()
    {
        TimeManager = TimeManager.Instance;
    }

    public void Initialise(PlayerController playerController, PlayerInput playerInput, Camera playerCamera, ulong LocalClientID)
    {
        this.playerController = playerController;
        this.playerInput = playerInput;
        EnableInput();
        this.playerCamera = playerCamera;
        playerConsoleManager = playerController.GetComponent<PlayerConsoleManager>();
    }

    [ServerRpc]
    private void RequestOwnerChangeServerRpc(ulong ownerID)
    {
        GetComponent<NetworkObject>().ChangeOwnership(ownerID);
    }

    public void SetToControl() => CanControl = true;
    public void ReleaseControl() => CanControl = false;

    private void OnEnable()
    {
        if (playerInput != null)
            EnableInput();
    }

    private void EnableInput()
    {
        playerInput.KeyboardMouse.Jump.started += Jump;
        playerInput.KeyboardMouse.Scope.started += ScopeSet;
        playerInput.KeyboardMouse.Fire.started += Fire;
        playerInput.Enable();
    }

    private void OnDisable()
    {
        if (playerInput == null) return;

        playerInput.KeyboardMouse.Jump.started -= Jump;
        playerInput.KeyboardMouse.Scope.started -= ScopeSet;
        playerInput.KeyboardMouse.Fire.started -= Fire;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;
        if (other.CompareTag("KillSwitch"))
        {
            if (other.TryGetComponent<KillSwitch>(out var killSwitch))
            {
                var team = killSwitch.GetTeamNumber;
                if (team != playerController.TeamNumber)
                {
                    var message = $"Game Over - {playerController.Name.Value} wins!";
                    print(message);
                    playerConsoleManager.LogMessage(message, "Server");
                    TimeManager.SetGameEnded();
                    ShowGameEndClientRpc(true);
                    isDead = true;
                }
            }
        }
    }

    private void Update()
    {
        if (!HasControl() || isDead) return;

        Rotate();
    }

    private void FixedUpdate()
    {
        var gravity = -gravityScale * Time.deltaTime;
        rigidBody.AddForce(Vector3.up * gravity, ForceMode.Force);

        if (!CanControl || !IsOwner) return;

        if (Health.Value <= 0 && !isDead)
        {
            isDead = true;
            if (playerCamera)
            {
                playerCamera.transform.parent = null;
            }
            Cursor.lockState = CursorLockMode.None;
            playerController.SetGamePausedServerRpc(true);
            playerController.SpawnNewUnit();

            OnDisable();
            DespawnUnitServerRpc();
            return;
        }

        if (!HasControl() || TimeManager.IsGamePaused)
        {
            if (TimeManager.IsGamePaused && HasControl())
            {
                if (playerInput.KeyboardMouse.Move.ReadValue<Vector2>().magnitude != 0)
                    playerController.SetGamePausedServerRpc(false);
            }
            return;
        }

        Run();
    }

    [ServerRpc]
    private void DespawnUnitServerRpc()
    {
        GetComponent<NetworkObject>().Despawn(true);
    }

    private bool HasControl()
    {
        return CanControl && IsOwner && playerController.isPlayersTurn;
    }

    private void Rotate()
    {
        isGrounded = Physics.CheckSphere(groundCheckPos.position, .05f, groundMask, QueryTriggerInteraction.Ignore);

        var mouseDelta = playerInput.KeyboardMouse.PointerDelta.ReadValue<Vector2>();
        var mouseX = mouseDelta.x * horizontalRotateSpeed * Time.deltaTime;
        var mouseY = (invertY ? mouseDelta.y : -mouseDelta.y) * verticalRotateSpeed * Time.deltaTime;

        xRotation += mouseY;
        xRotation = Mathf.Clamp(xRotation, -85f, 85f);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        newRotation.y += mouseX;
        var quatRotation = Quaternion.Euler(newRotation);
        transform.rotation = Quaternion.Slerp(transform.rotation, quatRotation, horizontalRotateSpeed * Time.deltaTime);
    }

    private void Run()
    {
        Vector3 moveValue = playerInput.KeyboardMouse.Move.ReadValue<Vector2>();

        moveValue.z = moveValue.y;
        moveValue.y = 0;
        var moveDirection = transform.right * moveValue.x + transform.forward * moveValue.z;
        moveDirection *= moveSpeed * Time.deltaTime;
        transform.Translate(moveDirection, Space.World);

        foreach (var stepCheck in stepChecks)
        {
            if (Physics.Raycast(stepCheck.position, Vector3.down, out var hitInfo, stepHeight, groundMask))
            {
                transform.position += Vector3.up * (stepHeight - hitInfo.distance);
                break;
            }
        }
    }

    private void Jump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (!isGrounded || !HasControl())
                return;
            var velocity = rigidBody.velocity;
            velocity.y = jumpForce;
            rigidBody.velocity = velocity;
        }
    }

    [ServerRpc]
    private void SubmitPositionRequestServerRpc(Vector3 newPos, ServerRpcParams rpcParams = default)
    {
        Position.Value = newPos;
    }

    private void ScopeSet(InputAction.CallbackContext context)
    {
        if (context.started)
            scope = true;
        else if (context.canceled)
            scope = false;
    }

    private void Fire(InputAction.CallbackContext context)
    {
        if (context.started && HasControl())
        {
            ReceiveDamageServerRpc(transform.position, playerCamera.transform.forward, swapPlayerOnShoot);
        }
    }

    [ServerRpc]
    public void ReceiveDamageServerRpc(Vector3 pos, Vector3 dir, bool swapPlayer)
    {
        Debug.DrawRay(pos, dir, Color.red, 5f);
        var hitResults = Physics.RaycastAll(pos, dir, 100f);
        if (hitResults.Length > 0)
        {
            foreach (var hitResult in hitResults)
            {
                if (hitResult.transform.TryGetComponent<Unit>(out Unit other))
                {
                    other.TakeDamage(50);
                    if (playerController)
                        playerController.GetComponent<PlayerConsoleManager>().LogMessage($"Dealt {other.Health.Value} damage", "Server");
                }
            }
        }

        if (swapPlayer)
        {
            TimeManager.Instance.SetNextPlayerServerRpc();
            TimeManager.Instance.ResetTimerServerRpc();
        }
        else if (TimeManager.Instance.IsGamePaused)
        {
            TimeManager.Instance.SetGamePaused(false);
        }
    }

    public void TakeDamage(float amount)
    {
        Health.Value -= amount;
    }

    [ClientRpc]
    public void ShowGameEndClientRpc(bool value)
    {
        UIManager.Instance.ShowGameEnd(value);
    }
}
