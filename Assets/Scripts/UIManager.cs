using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public TMP_InputField inputPlayerName;
    public Button btnDeselect;
    public Button btnChoose;
    public Button btnDisconnect;

    void Awake()
    {
        Instance = this;
    }
}
