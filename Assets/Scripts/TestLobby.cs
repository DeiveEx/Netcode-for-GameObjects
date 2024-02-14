using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Random = UnityEngine.Random;

public class TestLobby : MonoBehaviour
{
    private Lobby _joinedLobby;
    private string _lobbyCode;
    private bool _createPrivateLobby;
    private string _playerName;
    private string _gameMode = "DeathMatch";
    
    private async void Start()
    {
        _playerName = $"Player_{Random.Range(10, 99)}";
        Debug.Log($"PlayerName: {_playerName}");
        
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log($"Signed In with ID: {AuthenticationService.Instance.PlayerId}");
        };
        
        await AuthenticationService.Instance.SignInAnonymouslyAsync(); //TODO Use Steam account
        
        StartCoroutine(LobbyHeartbeatRoutine());
        StartCoroutine(LobbyPoolForUpdatesRoutine());
    }

    private async void CreateLobby()
    {
        string lobbyName = "MyLobby";
        int maxPlayers = 4;
        var lobbyOptions = new CreateLobbyOptions()
        {
            IsPrivate = _createPrivateLobby,
            Player = GetPlayer(),
            Data = new()
            {
                {"GameMode", new DataObject(DataObject.VisibilityOptions.Public, _gameMode, DataObject.IndexOptions.S1)} //S1 is an index you can use for filters.
            }
        };

        try
        {
            _joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, lobbyOptions);
            Debug.Log($"Created lobby '{_joinedLobby.Name}' for {_joinedLobby.MaxPlayers} players; IsPrivate: {_joinedLobby.IsPrivate}; ID: {_joinedLobby.Id}; Join Code: {_joinedLobby.LobbyCode}");
            PrintPlayers(_joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    private async void ListLobbies()
    {
        try
        {
            var options = new QueryLobbiesOptions()
            {
                Count = 25, //The max amount of lobbies to we want to get
                Filters = new List<QueryFilter>() //Filters to apply to the lobbies
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT), //This will filter lobbies that have 0 or more (GT = GreaterThan) available slots
                    new QueryFilter(QueryFilter.FieldOptions.S1, _gameMode, QueryFilter.OpOptions.EQ) //Here we're using the custom fields we created
                },
                Order = new List<QueryOrder>() //How should we order the lobbies found
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };
            
            var queryResponse = await Lobbies.Instance.QueryLobbiesAsync(options);

            StringBuilder sb = new StringBuilder($"Lobbies found: {queryResponse.Results.Count}");
            sb.AppendLine();

            foreach (var lobby in queryResponse.Results)
            {
                sb.Append($"- {lobby.Name}");
                sb.AppendLine();
                sb.Append($"{lobby.Players.Count}/{lobby.MaxPlayers} players");
                sb.AppendLine();
                sb.Append($"GameMode: {lobby.Data["GameMode"].Value}");
            }
        
            Debug.Log(sb);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }
    
    private async void JoinLobby(string lobbyCode)
    {
        try
        {
            var options = new JoinLobbyByCodeOptions()
            {
                Player = GetPlayer(),
            };
            
            _joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(_lobbyCode, options); //You can use JoinLobbyByIDAsync too
            Debug.Log($"Joined lobby: {_joinedLobby.Name}");
            PrintPlayers(_joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    private async void QuickJoinLobby()
    {
        try
        {
            var options = new QuickJoinLobbyOptions()
            {
                Player = GetPlayer(),
            };
            
            //Just join any lobby. You can alter options to filter which lobbies can be "quick joined"
            _joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            Debug.Log($"Joined lobby: {_joinedLobby.Name}");
            PrintPlayers(_joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));

        _createPrivateLobby = GUILayout.Toggle(_createPrivateLobby, "Make Lobby Private");

        if (_joinedLobby == null)
        {
            if (GUILayout.Button("Create Lobby"))
                CreateLobby();
        
            if (GUILayout.Button("List Lobbies"))
                ListLobbies();

            _lobbyCode = GUILayout.TextField(_lobbyCode);
        
            if (GUILayout.Button("Join Lobby"))
                JoinLobby(_lobbyCode);
        
            if (GUILayout.Button("Quick Join Lobby"))
                QuickJoinLobby();
        }
        else
        {
            if (GUILayout.Button("Print Players"))
                PrintPlayers(_joinedLobby);
        
            _playerName = GUILayout.TextField(_playerName);
            if (GUILayout.Button("Update Player Name"))
                UpdatePlayerName(_joinedLobby, _playerName);
            
            if (IsHost(_joinedLobby))
            {
                _gameMode = GUILayout.TextField(_gameMode);
                if (GUILayout.Button("Update GameMode"))
                    UpdateLobbyGameMode(_joinedLobby, _gameMode);
                
                GUILayout.Label("Players:");

                foreach (var lobbyPlayer in _joinedLobby.Players)
                {
                    if (GUILayout.Button($"Kick {lobbyPlayer.Data["PlayerName"].Value}"))
                        KickPlayer(lobbyPlayer.Id);
                }
            }
        }
        
        GUILayout.EndArea();
    }

    private IEnumerator LobbyHeartbeatRoutine()
    {
        //Lobbies become inactive after a certain time, meaning new players cannot find it (but players that already
        //joined can still get data from it). In order to keep the lobby alive, we need to send a "heartbeat", saying
        //that this lobby is still "alive" and active and it should be found by new players
        while (true)
        {
            yield return new WaitForSeconds(15);
            SendLobbyHeartbeat();
        }
    }

    async void SendLobbyHeartbeat()
    {
        if(_joinedLobby == null || !IsHost(_joinedLobby))
            return;
        
        try
        {
            Debug.Log("Sending heartbeat");
            await LobbyService.Instance.SendHeartbeatPingAsync(_joinedLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
            throw;
        }
    }

    private void PrintPlayers(Lobby lobby)
    {
        StringBuilder sb = new($"Lobby '{lobby.Name}':");
        sb.AppendLine();
        sb.Append($"GameMode: {lobby.Data["GameMode"].Value}");
        sb.AppendLine();
        sb.Append($"Players:");
        sb.AppendLine();
        
        foreach (var lobbyPlayer in lobby.Players)
        {
            sb.Append($"- ");

            if (IsHost(lobby, lobbyPlayer.Id))
                sb.Append($"[HOST] ");
            
            sb.Append($"{lobbyPlayer.Data["PlayerName"].Value}; ");
            sb.Append($"({lobbyPlayer.Id}); ");
            sb.AppendLine();
        }

        Debug.Log(sb);
    }

    private Player GetPlayer()
    {
        return new Player()
        {
            Data = new()
            {
                {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, _playerName)}
            }
        };
    }

    private async void UpdateLobbyGameMode(Lobby lobby, string newGameMode)
    {
        if(!IsHost(lobby))
            return;
        
        try
        {
            //Only update the GameMode, you don't need to reassign existing values
            var gameMode = lobby.Data["GameMode"];
            
            var options = new UpdateLobbyOptions()
            {
                Data = new ()
                {
                    {"GameMode", new DataObject(gameMode.Visibility, newGameMode, gameMode.Index)}
                }
            };
            
            _joinedLobby = await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, options);
            
            PrintPlayers(_joinedLobby);
            
            var playerID = AuthenticationService.Instance.PlayerId;
            if (_joinedLobby != null && (_joinedLobby.Players == null || _joinedLobby.Players.All(x => x.Id != playerID)))
            {
                Debug.Log("You left the lobby");
                _joinedLobby = null;
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }
    
    private async void UpdateLobbyHost(Lobby lobby, string hostID)
    {
        //We could call this method if we want to manually change the host
        try
        {
            var options = new UpdateLobbyOptions()
            {
                HostId = hostID
            };
            
            _joinedLobby = await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, options);
            
            PrintPlayers(_joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    private IEnumerator LobbyPoolForUpdatesRoutine()
    {
        //Changes in a lobby is not automatically propagated to players, so we need to pool for changes manually
        while (true)
        {
            yield return new WaitForSeconds(1.5f);
            LobbyPoolForUpdates();
        }
    }

    private async void LobbyPoolForUpdates()
    {
        if(_joinedLobby == null)
            return;
        
        try
        {
            _joinedLobby = await LobbyService.Instance.GetLobbyAsync(_joinedLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    private async void UpdatePlayerName(Lobby lobby, string newPlayerName)
    {
        try
        {
            var playerID = AuthenticationService.Instance.PlayerId;
            var playerName = lobby.Players.First(x => x.Id == playerID).Data["PlayerName"];
            
            var options = new UpdatePlayerOptions()
            {
                Data = new ()
                {
                    {"PlayerName", new PlayerDataObject(playerName.Visibility, newPlayerName)}
                }
            };
            
            _joinedLobby = await LobbyService.Instance.UpdatePlayerAsync(lobby.Id, playerID, options);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    private bool IsHost(Lobby lobby, string playerID = null)
    {
        if(playerID == null)
            playerID = AuthenticationService.Instance.PlayerId;
        
        return lobby.HostId == playerID;
    }

    private async void LeaveLobby()
    {
        try
        {
            var playerID = AuthenticationService.Instance.PlayerId;
            await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, playerID);
            _joinedLobby = null;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    private async void KickPlayer(string playerID)
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, playerID);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    private async void DeleteLobby()
    {
        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(_joinedLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }
}
