using System.Collections;
using UnityEngine;
using MLAPI;
using MLAPI.Serialization.Pooled;
using MLAPI.Messaging;
using MLAPI.Transports;
using System.IO;
using System.Text;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerConsoleManager : NetworkBehaviour
{
    public static PlayerConsoleManager Instance;
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
        playerInput = GetComponent<PlayerController>().playerInput;

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
        playerInput.Enable();
    }

    private void OnDisable() {
        if (IsLocalPlayer && ConsoleLogger)
            ConsoleLogger.gameObject.SetActive(false);
        
        if (playerInput == null) return;
        playerInput.KeyboardMouse.SendMessage.started -= SendMessage;
        playerInput.KeyboardMouse.ToggleChat.started -= ToggleChat;
        playerInput.Disable();
    }

    private void ToggleChat(InputAction.CallbackContext context)
    {
        if (inputField.isFocused)
            return;
        inputField.Select();
        var active = ConsoleLogger.gameObject.activeInHierarchy;
        Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
        ConsoleLogger.gameObject.SetActive(!active);
    }

    public void SendMessage(InputAction.CallbackContext context)
    {
        if (inputField.text == "") return;        
        LogMessage(inputField.text);
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

    public void LogMessage(string messageToSend)
    {
        var buffer = SetMessageToSend(messageToSend);
        foreach(var client in NetworkManager.ConnectedClients)
        {
            CustomMessagingManager.SendNamedMessage("MessageName", client.Key, buffer);
        }
    }

    public void LogMessage(string messageToSend, ulong clientID)
    {
        var buffer = SetMessageToSend(messageToSend);
        print(clientID.ToString());
        CustomMessagingManager.SendNamedMessage("MessageName", clientID, buffer);
    }
}
