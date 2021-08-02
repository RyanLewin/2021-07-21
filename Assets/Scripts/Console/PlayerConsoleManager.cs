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
    PlayerInput playerInput;
    string message;
    NetworkChannel channel;
    [SerializeField] TMP_InputField inputField;

    private void OnGUI() {
        if (!IsLocalPlayer) return;
        GUILayout.Space(50f);
        GUILayout.Label("Message: " + message);
    }

    public void SetPlayerInput(PlayerInput _playerInput)
    {
        playerInput = _playerInput;
        EnableInput();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!IsLocalPlayer) return;
        inputField = ConsoleLogger.Instance.inputField;

        channel = new NetworkChannel();
        playerInput = GetComponent<PlayerController>().playerInput;

        CustomMessagingManager.RegisterNamedMessageHandler("myMessageName", (senderClientID, stream) =>
        {
            using (PooledNetworkReader reader = PooledNetworkReader.Get(stream))
            {
                var stringBuilder = reader.ReadString();
                message = stringBuilder.ToString();
                ConsoleLogger.Instance.AddMessage(message);
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
        message = messageToSend;
        ConsoleLogger.Instance.AddMessage(message);
        var buffer = PooledNetworkBuffer.Get();
        using (var writer = PooledNetworkWriter.Get(buffer))
        {
            writer.WriteString(message);
        }
        CustomMessagingManager.SendNamedMessage("myMessageName", NetworkManager.ServerClientId, buffer, channel);
    }
}
