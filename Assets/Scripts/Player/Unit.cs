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

    bool scope = false;

    public NetworkVariableVector3 Position = new NetworkVariableVector3(new NetworkVariableSettings
    {
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });

    private void OnGUI()
    {
        GUILayout.Space(50);
        if (playerController)
            GUILayout.Label(playerController.canPlayerMove./* Value. */ToString());
        // GUILayout.Label(IsOwner.ToString());
    }

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
        SubmitPositionRequestServerRpc(transform.position);
    }

    public void Initialise(PlayerController playerController, PlayerInput playerInput, Camera playerCamera, ulong LocalClientID)
    {
        this.playerController = playerController;
        this.playerInput = playerInput;
        EnableInput();
        this.playerCamera = playerCamera;

        // RequestOwnerChangeServerRpc(LocalClientID);
    }

    [ServerRpc]
    private void RequestOwnerChangeServerRpc(ulong ownerID)
    {
        GetComponent<NetworkObject>().ChangeOwnership(ownerID);
    }

    public void SetToControl()
    {
        CanControl = true;
        // Cursor.lockState = CursorLockMode.Locked;
    }

    public void ReleaseControl()
    {
        CanControl = false;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnEnable()
    {
        if (playerInput != null)
            EnableInput();
    }

    private void EnableInput()
    {
        playerInput.KeyboardMouse.Jump.started += Jump;
        playerInput.KeyboardMouse.Scope.started += ScopeSet;
        playerInput.Enable();
    }

    private void OnDisable()
    {
        if (playerInput == null) return;

        playerInput.KeyboardMouse.Jump.started -= Jump;
        playerInput.KeyboardMouse.Scope.started -= ScopeSet;
        // playerInput.Disable();
    }

    private void Update()
    {
        if (!HasControl()) return;

        Rotate();
    }

    private void FixedUpdate()
    {
        var gravity = -gravityScale * Time.deltaTime;
        rigidBody.AddForce(Vector3.up * gravity, ForceMode.Force);

        if (!HasControl()) return;
        Run();
    }

    private bool HasControl()
    {
        return CanControl && IsOwner && playerController.canPlayerMove;
    }

    private void Rotate()
    {
        isGrounded = Physics.CheckSphere(groundCheckPos.position, .05f, groundMask);

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

        foreach(var stepCheck in stepChecks)
        {
            if (Physics.Raycast(stepCheck.position, Vector3.down, out var hitInfo, stepHeight, groundMask))
            {
                transform.position += Vector3.up * (stepHeight - hitInfo.distance);
                break;
            }
        }
        SubmitPositionRequestServerRpc(transform.position/*  + moveDirection */);
    }

    private void Jump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (!isGrounded || !HasControl())
                return;
            GetComponent<Rigidbody>().AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
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
}
