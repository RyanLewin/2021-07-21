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

    bool isMouseDown = false;

    public NetworkVariableVector3 Position = new NetworkVariableVector3(new NetworkVariableSettings
    {
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });

    private void OnGUI()
    {
        GUILayout.Space(50);
        if (playerController)
            GUILayout.Label(playerController.CanPlayerMove.Value.ToString());
        // GUILayout.Label(IsOwner.ToString());
    }

    private void Awake()
    {
        newRotation = transform.eulerAngles;
        rigidBody = GetComponent<Rigidbody>();
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
        playerInput.Enable();
    }

    private void OnDisable()
    {
        if (playerInput != null)
            playerInput.KeyboardMouse.Jump.started -= Jump;
        // playerInput.Disable();
    }

    private void OnDrawGizmos() {
        Gizmos.DrawSphere(groundCheckPos.position, .05f);
    }

    private void Update()
    {
        if (!CanControl || !IsOwner || !playerController.CanPlayerMove.Value) return;

        isGrounded = Physics.CheckSphere(groundCheckPos.position, .05f, groundMask);

        var mouseDelta = playerInput.KeyboardMouse.PointerDelta.ReadValue<Vector2>();
        var mouseX = mouseDelta.x * horizontalRotateSpeed * Time.deltaTime;
        var mouseY = (invertY ? mouseDelta.y : -mouseDelta.y) * verticalRotateSpeed * Time.deltaTime;

        xRotation += mouseY;
        xRotation = Mathf.Clamp(xRotation, -85f, 85f);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        newRotation.y += mouseX /* * horizontalRotateSpeed * Time.deltaTime */;
        var quatRotation = Quaternion.Euler(newRotation);
        transform.rotation = Quaternion.Slerp(transform.rotation, quatRotation, horizontalRotateSpeed * Time.deltaTime);
        // transform.Rotate(Vector3.up * mouseX);

    }

    private void FixedUpdate()
    {
        var gravity = -gravityScale * Time.deltaTime;
        rigidBody.AddForce(Vector3.up * gravity, ForceMode.Force);

        if (!CanControl || !IsOwner || !playerController.CanPlayerMove.Value) return;
        Vector3 moveValue = playerInput.KeyboardMouse.Move.ReadValue<Vector2>();

        moveValue.z = moveValue.y;
        moveValue.y = 0;
        var moveDirection = transform.right * moveValue.x + transform.forward * moveValue.z;
        moveDirection *= moveSpeed * Time.deltaTime;
        // moveDirection.y = -gravityScale * Time.deltaTime;
        // rigidBody.AddForce(moveDirection * moveSpeed * Time.deltaTime);
        // transform.position = Vector3.Lerp(transform.position, transform.position + moveDirection, moveSpeed*Time.deltaTime);
        transform.Translate(moveDirection, Space.World);
        SubmitPositionRequestServerRpc(transform.position/*  + moveDirection */);
    }

    private void Jump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (!isGrounded || !CanControl ||
                !IsOwner || !playerController.CanPlayerMove.Value)
                return;
            GetComponent<Rigidbody>().AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    [ServerRpc]
    private void SubmitPositionRequestServerRpc(Vector3 newPos, ServerRpcParams rpcParams = default)
    {
        Position.Value = newPos;
    }

    private void MouseClick(InputAction.CallbackContext context)
    {
        if (context.started)
            isMouseDown = true;
        else if (context.canceled)
            isMouseDown = false;
    }
}
