using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using PlayFab;
using PlayFab.MultiplayerModels;
using PlayFab.DataModels;
using PlayFab.ClientModels;
using PlayFab.ProfilesModels;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

public class Matchmaker : MonoBehaviour
{
    [SerializeField] Configuration configuration;
    [SerializeField] GameObject playButton;
    [SerializeField] GameObject leaveQueueButton;
    [SerializeField] TMP_Text queueStatusText;

    string ticketId;
    const string QueueName = "SkillBased";
    const string PlayerType = "title_player_account";
    Coroutine pollingCoroutine;

    private void OnGUI() {
        if (GUILayout.Button("FindMatch"))
            StartMatchmaking();
    }

    public void StartMatchmaking()
    {
        if (playButton) playButton.SetActive(false);
        queueStatusText.text = $"Submitting ticket";
        queueStatusText.gameObject.SetActive(true);

        PlayFabMultiplayerAPI.CreateMatchmakingTicket(
            new CreateMatchmakingTicketRequest
            {
                Creator = new MatchmakingPlayer
                {
                    Entity = new PlayFab.MultiplayerModels.EntityKey
                    {
                        Id = PlayfabLogin.EntityId,
                        Type = PlayfabLogin.EntityType
                    },
                    Attributes = new MatchmakingPlayerAttributes
                    {
                        DataObject = new { 
                            Score = PlayfabLogin.playerScore,
                            Username = PlayfabLogin.displayName
                        }
                    }
                },
                GiveUpAfterSeconds = 120,

                QueueName = QueueName
            },
            OnMatchmakingTicketCreated,
            OnMatchmakingError
        );
    }

    private void OnMatchmakingTicketCreated(CreateMatchmakingTicketResult result)
    {
        ticketId = result.TicketId;
        if (leaveQueueButton) leaveQueueButton.SetActive(true);
        queueStatusText.text = $"Ticket Created";

        pollingCoroutine = StartCoroutine(PollTicket());
    }

    private void OnMatchmakingError(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
    }

    IEnumerator PollTicket()
    {
        while (true)
        {
            PlayFabMultiplayerAPI.GetMatchmakingTicket(
                new GetMatchmakingTicketRequest
                {
                    TicketId = ticketId,
                    QueueName = QueueName,
                },
                OnGetMathcmakingTicket,
                OnMatchmakingError
            );

            yield return new WaitForSeconds(6f);
        }
    }

    private void OnGetMathcmakingTicket(GetMatchmakingTicketResult result)
    {
        queueStatusText.text = $"Status: {result.Status}";

        switch(result.Status)
        {
            case "Matched":
                StopCoroutine(pollingCoroutine);
                StartMatch(result.MatchId);
                break;
            case "Canceled":
                break;
        }
    }

    public void CancelMatchmakingTicket()
    {
        PlayFabMultiplayerAPI.CancelMatchmakingTicket(new CancelMatchmakingTicketRequest() {
            QueueName = QueueName,
            TicketId = ticketId
        },
        OnMatchmakingCanceled,
        OnMatchmakingError);
    }

    private void OnMatchmakingCanceled(CancelMatchmakingTicketResult result)
    {
        queueStatusText.text = "Canceled Match Find";
        if (leaveQueueButton) leaveQueueButton.SetActive(false);
        if (playButton) playButton.SetActive(true);
    }

    private void StartMatch(string matchId)
    {
        queueStatusText.text = "Starting Match";

        PlayFabMultiplayerAPI.GetMatch(
            new GetMatchRequest
            {
                MatchId = matchId,
                QueueName = QueueName,
                ReturnMemberAttributes = true
            },
            OnGetMatch,
            OnMatchmakingError
        );
    }

    private void OnGetMatch(GetMatchResult result)
    {
        var data = result.Members[0].Attributes.DataObject.ToString();
        queueStatusText.text = data;
        var player0 = DisplayPlayerData(data);
        data = result.Members[1].Attributes.DataObject.ToString();
        var player1 = DisplayPlayerData(data);

        var message = player0 + "------------ vs ------------\n" + player1;
        print(message);
        queueStatusText.text = message; 

        PlayFabMultiplayerAPI.RequestMultiplayerServer(new RequestMultiplayerServerRequest() {
            BuildId = configuration.buildId,
            SessionId = System.Guid.NewGuid().ToString();
        })
        RequestMultiplayerServerRequest requestData = new RequestMultiplayerServerRequest();
        requestData.BuildId = configuration.buildId;
        requestData.InitialPlayers
        requestData.SessionId = System.Guid.NewGuid().ToString();
        requestData.PreferredRegions = new List<string>() { AzureRegion.WestEurope.ToString() };
        PlayFabMultiplayerAPI.RequestMultiplayerServer(requestData, OnRequestMultiplayerServer, OnRequestMultiplayerServerError);
    }

    private string DisplayPlayerData(string playerData)
    {
        playerData = playerData.Substring(1, playerData.Length - 2);
        var splitData = playerData.Split(',');
        // print(splitData[1].Split(':')[1].Replace("\"", ""));
        return $"Player: {splitData[1].Split(':')[1].Replace("\"", "")} --- Score: {splitData[0].Split(':')[1].Replace("\"", "")}\n";
    }
}
