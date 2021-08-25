using UnityEngine;
using Steamworks;
using System;
using System.Text;
using PlayFab;
using PlayFab.ClientModels;

public class SteamLogin : MonoBehaviour
{
    public static string PlayfabId;
    public static string Username;
    
    public string GetSteamAuthTicket()
    {
        byte[] ticketBlob = new byte[1024];
        uint ticketSize;

        HAuthTicket hAuthTicket = SteamUser.GetAuthSessionTicket(ticketBlob, ticketBlob.Length, out ticketSize);
        Array.Resize(ref ticketBlob, (int)ticketSize);

        StringBuilder sb = new StringBuilder();
        foreach(byte b in ticketBlob)
        {
            sb.AppendFormat("{0:x2}", b);
        }
        return sb.ToString();
    }

    public void LoginWithSteam()
    {
        if (SteamManager.Initialized)
        {
            PlayFabClientAPI.LoginWithSteam(new LoginWithSteamRequest 
            {
                CreateAccount = true,
                SteamTicket = GetSteamAuthTicket()
            },
            result => 
            {
                Debug.Log("Success");
                PlayfabId = result.PlayFabId;
                
                PlayFabClientAPI.GetPlayerProfile(new GetPlayerProfileRequest() {
                    PlayFabId = PlayfabId,
                    ProfileConstraints = new PlayerProfileViewConstraints{
                        ShowDisplayName = true,

                    }
                },
                result => {
                    Username = result.PlayerProfile.DisplayName;
                    print(Username + " logged in");
                },
                (error) => Debug.LogError(error.GenerateErrorReport()));
            },
            (error) => Debug.LogError(error.GenerateErrorReport()));
        }
    }

    // private void OnGUI() {
    //     if (GUILayout.Button("Login"))
    //     {
    //         LoginWithSteam();
    //     }
    // }
}
