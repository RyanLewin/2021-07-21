using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine.UI;
using PlayFab.DataModels;
using System.Collections.Generic;
using System;
using System.Collections;

public class PlayfabLogin : MonoBehaviour
{
    [SerializeField] GameObject mainMenu;
    [SerializeField] GameObject signInMenu;
    [SerializeField] TMP_InputField usernameInputField;
    [SerializeField] TMP_InputField emailInputField;
    [SerializeField] TMP_InputField passwordInputField;
    [SerializeField] Button submitButton;
    [SerializeField] TMP_Text txtPlayerDetails;
    [SerializeField] GameObject signUpButton;
    [SerializeField] GameObject signOutButton;
    [SerializeField] Button playButton;

    public static string SessionTicket;
    public static string PlayfabId;
    public static string EntityId;
    public static string EntityType;

    public static string displayName;
    public static int playerScore;

    public static GetUserDataResult playerData;

    private void Start() 
    {
        var username = PlayerSaveData.Instance ? PlayerSaveData.Instance.playerUsername : "";
        var password = PlayerSaveData.Instance ? PlayerSaveData.Instance.playerPassword : "";
        if (username != "" && password != "")
        {
            usernameInputField.text = username;
            passwordInputField.text = password;
            SignIn(username, password);
        }
        else 
        {
            CreateAnonymousPlayer();
        }
    }

    public void CreateAnonymousPlayer()
    {
        PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest()
        {
            CreateAccount = true,
            CustomId = "AnonPlayer"
        },
        result => {
            SessionTicket = result.SessionTicket;
            EntityId = result.EntityToken.Entity.Id;
            EntityType = result.EntityToken.Entity.Type;
            PlayfabId = result.PlayFabId;
            PlayFabClientAPI.UpdateUserTitleDisplayName(new UpdateUserTitleDisplayNameRequest() {
                DisplayName = "Guest"
            },
            result => {
                displayName = result.DisplayName;
                txtPlayerDetails.text = $"{displayName} created";
                GetPlayerData();
            },
            error => Debug.LogError(error.GenerateErrorReport()));
        },
        error => Debug.LogError(error.GenerateErrorReport()));
        // PlayFabClientAPI.LinkCustomID(new LinkCustomIDRequest()
        // {
        //     CustomId = 
        // })
    }

    public void ShowSignUpMenu(bool signUp)
    {
        emailInputField.transform.parent.gameObject.SetActive(signUp);
        signInMenu.SetActive(true);
        mainMenu.SetActive(false);
        submitButton.onClick.AddListener(() =>
        {
            if (signUp)
                CreateAccount();
            else
                SignIn();
        });
    }

    public void CreateAccount()
    {
        PlayFabClientAPI.RegisterPlayFabUser(new RegisterPlayFabUserRequest
        {
            Username = usernameInputField.text,
            Email = emailInputField.text,
            Password = passwordInputField.text,
            DisplayName = usernameInputField.text
        }, result =>
        {
            SessionTicket = result.SessionTicket;
            EntityId = result.EntityToken.Entity.Id;
            EntityType = result.EntityToken.Entity.Type;
            PlayfabId = result.PlayFabId;

            if (PlayerSaveData.Instance) PlayerSaveData.Instance.SetUsername(usernameInputField.text);
            if (PlayerSaveData.Instance) PlayerSaveData.Instance.SetPassword(passwordInputField.text);

            GetPlayerData();

            signInMenu.SetActive(false);
            mainMenu.SetActive(true);
        }, error =>
        {
            Debug.LogError(error.GenerateErrorReport());
        });
    }

    public void SignIn(string username = "", string password = "")
    {
        PlayFabClientAPI.LoginWithPlayFab(new LoginWithPlayFabRequest
        {
            Username = username == "" ? usernameInputField.text : username,
            Password = password == "" ? passwordInputField.text : password
        }, result =>
        {
            SessionTicket = result.SessionTicket;
            EntityId = result.EntityToken.Entity.Id;
            EntityType = result.EntityToken.Entity.Type;
            PlayfabId = result.PlayFabId;

            if (PlayerSaveData.Instance) PlayerSaveData.Instance.SetUsername(usernameInputField.text);
            if (PlayerSaveData.Instance) PlayerSaveData.Instance.SetPassword(passwordInputField.text);
            txtPlayerDetails.text = $"Logging in: {usernameInputField.text}.";

            GetPlayerData();

            signInMenu.SetActive(false);
            mainMenu.SetActive(true);
        }, error =>
        {
            Debug.LogError(error.GenerateErrorReport());
            txtPlayerDetails.text = $"Couldn't log in: {usernameInputField.text}.";
        });
    }

    public void GetPlayerData()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest()
        {
            PlayFabId = PlayfabId,
            Keys = null
        }, result =>
        {
            playerData = result;
            SetPlayerScore();

            if (playerData != null && playerData.Data.Count > 0)
            {
                //UI stuff
                playButton.interactable = true;
                signUpButton.SetActive(false);
                signOutButton.SetActive(true);
            }
        },
        error =>
        {
            Debug.LogError(error.GenerateErrorReport());
            txtPlayerDetails.text = $"Couldn't find player data: {usernameInputField.text}.";
            SetPlayerScore();
        });
    }

    void SetPlayerScore()
    {
        //Default score if player doesn't have one already and can't be found from the title data
        string score = "1000";

        //if player has a score already, then use that, else get the base from title data
        if (playerData != null && playerData.Data.ContainsKey("Score"))
        {
            displayName = playerData.Data["Username"].Value;
            if (displayName == "" || displayName == null)
                displayName = "Guest";
            score = playerData.Data["Score"].Value;
            SetPlayerData(score);

            playerScore = int.Parse(score);
            if (PlayerSaveData.Instance) PlayerSaveData.Instance.SetPoints(playerScore);
            txtPlayerDetails.text = $"Logged In: {displayName} --- Score: {playerScore}";
        }
        else
        {
            txtPlayerDetails.text = $"Creating new player data: {usernameInputField.text}.";
            GetBaseScore(score);
        }
    }

    void GetBaseScore(string score)
    {
        //Attempt to get the base score from playfab's title data
        PlayFabClientAPI.GetTitleData(new GetTitleDataRequest(),
        result => {
            if (result.Data == null || !result.Data.ContainsKey("BaseScore"))
                Debug.LogError("No Base Score");
            else
                score = result.Data["BaseScore"];
            SetPlayerData(score);
        },
        error => {
            txtPlayerDetails.text = "Couldn't find title data.";
            Debug.LogError($"Missing Title Data: {error.GenerateErrorReport()}");
            SetPlayerData(score);
        });

    }

    void SetPlayerData(string score)
    {
        var data = new Dictionary<string, string>()
        {
            {"Username", usernameInputField.text},
            {"Score", score}
        };
    
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest()
        {
            Data = data,
            Permission = UserDataPermission.Public
        },
        result => {
            Debug.Log("Successfully updated user data --- Score: " + data["Score"]);
            // txtPlayerDetails.text = $"Username: {data["Username"]} --- Score: {data["Score"]}";
            // playButton.interactable = true;
            // signUpButton.SetActive(false);
            // signOutButton.SetActive(true);
            
            //Try get user data again.
            if (playerData == null || playerData.Data.Count == 0)
            {
                txtPlayerDetails.text = $"Logged In: {data["Username"]}. Fetching Player data.";
                PlayFabClientAPI.GetUserData(new GetUserDataRequest()
                {
                    PlayFabId = PlayfabId,
                    Keys = null
                }, result => {
                    playerData = result;
                    playerScore = int.Parse(playerData.Data["Score"].Value);
                    displayName = playerData.Data["Username"].Value;
                    if (displayName == "" || displayName == null)
                        displayName = "Guest";

                    //UI things
                    txtPlayerDetails.text = $"Logged In: {displayName} --- Score: {playerScore}";
                    playButton.interactable = true;
                    signUpButton.SetActive(false);
                    signOutButton.SetActive(true);
                },
                error =>{
                    Debug.LogError(error.GenerateErrorReport());
                    txtPlayerDetails.text = $"Can't find player data; Try log in again";
                });
            }
        },
        error => {
            Debug.LogError(error.GenerateErrorReport());
            txtPlayerDetails.text = $"Couldn't set user data: {data["Username"]}";
        });
    }

    public void SignOut()
    {
        playButton.interactable = false;
        signUpButton.SetActive(true);
        signOutButton.SetActive(false);
        
        //Logs player out
        PlayFabClientAPI.ForgetAllCredentials();
        if (PlayerSaveData.Instance) PlayerSaveData.Instance.SetUsername("");
        if (PlayerSaveData.Instance) PlayerSaveData.Instance.SetPassword("");

        playerData = null;
        txtPlayerDetails.text = "Not Logged In";
    }
}
