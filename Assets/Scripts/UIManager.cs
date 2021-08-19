using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public GameObject objMenuUI;
    public GameObject unitSelectionUI;
    public GameObject playUI;
    public GameObject gameEndUI;

    [Space(5f)]
    public TMP_InputField inputPlayerName;

    [Space(5f)]
    public Button btnDeselect;
    public Button btnChoose;
    public Button btnDisconnect;
    public TextMeshProUGUI timerText;
    public Button btnGameEndDisconnect;

    UIWindows currentOpenUI;

    void Awake()
    {
        if (UIManager.Instance)
            Destroy(UIManager.Instance.gameObject);
        Instance = this;
    }

    private void Start() {
        timerText.text = $"Timer: {TimeManager.Instance.timePerTurn.ToString("0.00")}";
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

    public void SetUI(UIWindows UIToOpen)
    {
        currentOpenUI = UIToOpen;
        objMenuUI.SetActive(UIToOpen == UIWindows.Menu ? true : false);
        unitSelectionUI.SetActive(UIToOpen == UIWindows.UnitSelection ? true : false);
        playUI.SetActive(UIToOpen == UIWindows.PlayUI ? true : false);
        gameEndUI.SetActive(UIToOpen == UIWindows.GameEndUI ? true : false);
    }

    public void SetLockMode()
    {
        print(currentOpenUI);
        switch(currentOpenUI)
        {
            case(UIWindows.PlayUI):
                Cursor.lockState = CursorLockMode.Locked;
                break;
            default:
                Cursor.lockState = CursorLockMode.None;
                break;
        }
    }
}

public enum UIWindows
{
    Menu,
    UnitSelection,
    PlayUI,
    GameEndUI,
    None
}
