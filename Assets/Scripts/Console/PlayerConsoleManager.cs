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

    private void OnGUI() {
        GUILayout.Space(50);
        GUILayout.Label(IsOwner.ToString());
    }

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

    // Start is called before the first frame update
    public override void NetworkStart()
    {
        if (!IsLocalPlayer) return;
        inputField = ConsoleLogger.inputField;

        channel = new NetworkChannel();
        playerInput = GetComponent<PlayerController>().playerInput;

        CustomMessagingManager.RegisterNamedMessageHandler("myMessageName", (senderClientID, stream) =>
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
        playerInput.Enable();
    }

    private void OnDisable() {
        if (playerInput == null) return;
        playerInput.KeyboardMouse.SendMessage.started -= SendMessage;
        playerInput.Disable();
    }

    public void SendMessage(InputAction.CallbackContext context)
    {
        if (inputField.text == "") return;        
        LogMessage(inputField.text);
        inputField.text = "";
    }

    public void LogMessage(string messageToSend)
    {
        latestMessage = messageToSend;
        ConsoleLogger.AddMessage(latestMessage);
        var buffer = PooledNetworkBuffer.Get();
        using (var writer = PooledNetworkWriter.Get(buffer))
        {
            writer.WriteString(latestMessage);
        }
        
        foreach(var client in NetworkManager.ConnectedClients)
        {
            CustomMessagingManager.SendNamedMessage("myMessageName", client.Key, buffer);
        }
    }
}
