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

    public void SetDisconnectButton(PlayerController playerController)
    {
        btnGameEndDisconnect.onClick.AddListener(() => 
        {
            if (playerController != null)
                playerController.DisconnectPlayer();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
        });
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
}

public enum UIWindows
{
    Menu,
    PlayUI,
    GameEndUI
}
