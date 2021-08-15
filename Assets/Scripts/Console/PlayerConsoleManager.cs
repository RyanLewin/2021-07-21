using System.Collections;
using UnityEngine;
using MLAPI;
using MLAPI.Serialization.Pooled;
using MLAPI.Messaging;
using MLAPI.Transports;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.EventSystems;

public class PlayerConsoleManager : NetworkBehaviour
{
    public static PlayerConsoleManager Instance;
    string playerName;
    ConsoleLogger ConsoleLogger;
    PlayerInput playerInput;
    string latestMessage;
    string debug;
    NetworkChannel channel;
    [SerializeField] TMP_InputField inputField;

    private void Awake() 
    {
        if (IsOwner)
        {
            Instance = this;
        }
        ConsoleLogger = ConsoleLogger.Instance;
    }

    public void SetPlayerInput(PlayerInput _playerInput)
    {
        playerInput = _playerInput;
        EnableInput();
    }

    public override void NetworkStart()
    {
        if (!IsLocalPlayer) return;
        // ConsoleLogger.gameObject.SetActive(true);
        inputField = ConsoleLogger.inputField;

        channel = new NetworkChannel();
        var playerController = GetComponent<PlayerController>();
        playerInput = playerController.playerInput;

        CustomMessagingManager.RegisterNamedMessageHandler("MessageName", (senderClientID, stream) =>
        {
            using (PooledNetworkReader reader = PooledNetworkReader.Get(stream))
            {
                var stringBuilder = reader.ReadString();
                latestMessage = stringBuilder.ToString();
                ConsoleLogger.Instance.AddMessage(latestMessage);
            }
        });
    }

    public void SetPlayerName(string name)
    {
        playerName = name;
    }

    private void OnEnable() 
    {
        EnableInput();
    }

    private void EnableInput() 
    {
        if (playerInput == null ) 
            return;
        playerInput.KeyboardMouse.SendMessage.started += SendMessage;
        playerInput.KeyboardMouse.ToggleChat.started += ToggleChat;
        playerInput.KeyboardMouse.CloseChat.started += CloseChat;
        playerInput.KeyboardMouse.FocusTextInput.started += FocusChatInput;
        playerInput.KeyboardMouse.ScrollChat.started += ScrollChat;
        playerInput.Enable();
    }

    private void OnDisable() {
        if (IsLocalPlayer && ConsoleLogger)
            ConsoleLogger.SetShowFullChat(false);
        
        if (playerInput == null) return;
        playerInput.KeyboardMouse.SendMessage.started -= SendMessage;
        playerInput.KeyboardMouse.ToggleChat.started -= ToggleChat;
        playerInput.KeyboardMouse.CloseChat.started -= CloseChat;
        playerInput.KeyboardMouse.FocusTextInput.started -= FocusChatInput;
        playerInput.KeyboardMouse.ScrollChat.started -= ScrollChat;
        playerInput.Disable();
    }

    private void ScrollChat(InputAction.CallbackContext context)
    {
        if (!ConsoleLogger.openChat.activeInHierarchy)
            return;
        ConsoleLogger.ScrollText(context.ReadValue<float>() > 0 ? 1 : -1);
    }

    private void FocusChatInput(InputAction.CallbackContext context)
    {
        if (!ConsoleLogger.openChat.activeInHierarchy)
            return;
        inputField.ActivateInputField();
        inputField.Select();
    }

    private void CloseChat(InputAction.CallbackContext context)
    {
        Cursor.lockState = CursorLockMode.Locked;
        ConsoleLogger.SetShowFullChat(false);
    }

    private void ToggleChat(InputAction.CallbackContext context)
    {
        if (inputField.isFocused)
            return;
            
        inputField.ActivateInputField();
        inputField.Select();
        var active = ConsoleLogger.openChat.activeInHierarchy;
        Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
        ConsoleLogger.SetShowFullChat(!active);
    }

    public void SendMessage(InputAction.CallbackContext context)
    {
        if (inputField.text == "") return;
        LogMessage(inputField.text, playerName);
        inputField.text = "";
    }

    private PooledNetworkBuffer SetMessageToSend(string messageToSend)
    {
        latestMessage = messageToSend;
        ConsoleLogger.AddMessage(latestMessage);
        var buffer = PooledNetworkBuffer.Get();
        var writer = PooledNetworkWriter.Get(buffer);
        writer.WriteString(latestMessage);
        return buffer;
    }

    public void LogMessage(string messageToSend, string senderName)
    {
        messageToSend = $"{senderName}: {messageToSend}";
        var buffer = SetMessageToSend(messageToSend);
        foreach(var client in NetworkManager.ConnectedClients)
        {
            CustomMessagingManager.SendNamedMessage("MessageName", client.Key, buffer);
        }
    }

    public void LogMessage(string messageToSend, string senderName, ulong clientID)
    {
        messageToSend = $"{senderName}: {messageToSend}";
        var buffer = SetMessageToSend(messageToSend);
        print(clientID.ToString());
        CustomMessagingManager.SendNamedMessage("MessageName", clientID, buffer);
    }
}
