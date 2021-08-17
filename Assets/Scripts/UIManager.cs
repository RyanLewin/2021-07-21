using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public GameObject objMenuUI;
    public GameObject objPlayUI;
    public GameObject gameEndUI;

    [Space(5f)]
    public TMP_InputField inputPlayerName;
    public Button btnHost;
    public Button btnJoin;
    public Button btnServer;

    [Space(5f)]
    public Button btnDeselect;
    public Button btnChoose;
    public Button btnDisconnect;
    public TextMeshProUGUI timerText;
    public Button btnGameEndDisconnect;

    void Awake()
    {
        if (UIManager.Instance)
            Destroy(UIManager.Instance.gameObject);
        Instance = this;
    }

    private void Start()
    {
        // btnHost.onClick.RemoveAllListeners();
        // btnJoin.onClick.RemoveAllListeners();
        // btnServer.onClick.RemoveAllListeners();

        // btnHost.onClick.AddListener(() => {SetUI(UIWindows.PlayUI);});
        // btnHost.onClick.AddListener(LewinNetworkManager.Instance.HostGame);
        // btnJoin.onClick.AddListener(() => {SetUI(UIWindows.PlayUI);});
        // btnJoin.onClick.AddListener(LewinNetworkManager.Instance.JoinGame);
        // btnServer.onClick.AddListener(() => {SetUI(UIWindows.PlayUI);});
        // btnServer.onClick.AddListener(LewinNetworkManager.Instance.HostServer);
    }

    public void ShowGameEnd(bool value)
    {
        gameEndUI.gameObject.SetActive(value);
    }

    public void SetUI(UIWindows UIToOpen)
    {
        objMenuUI.SetActive(UIToOpen == UIWindows.Menu ? true : false);
        objPlayUI.SetActive(UIToOpen == UIWindows.PlayUI ? true : false);
        gameEndUI.SetActive(UIToOpen == UIWindows.GameEndUI ? true : false);
    }

    // private void FixedUpdate()
    // {

    // }
}

public enum UIWindows
{
    Menu,
    PlayUI,
    GameEndUI
}
