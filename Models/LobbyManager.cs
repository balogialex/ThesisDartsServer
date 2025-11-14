using System;
using System.Collections.Generic;
using System.Linq;
using DartsAPI.Dtos;
using DartsAPI.Models;

namespace DartsAPI.Models;

public static class LobbyManager
{
    private static readonly List<Lobby> Lobbies = new List<Lobby>();

    public static Lobby CreateLobby(CreateLobbyDto createDto)
    {
        if (IsPlayerInLobby(createDto.LobbyCreator))
            throw new InvalidOperationException("Player is already in a lobby.");

        var lobby = new Lobby
        {
            Title = createDto.LobbyTitle,
            Creator = createDto.LobbyCreator,
            MaxPlayers = 2,
            Settings = createDto.Settings
        };
        lobby.Players.Add(createDto.LobbyCreator);
        lobby.PlayerReadiness[createDto.LobbyCreator] = false;
        Lobbies.Add(lobby); 


        return lobby;
    }
    public static List<Lobby> GetAllLobbies()
    {
        return Lobbies.Where(l => l.Status == LobbyStatus.Created).ToList();
    }
    public static Lobby GetLobby(string lobbyGUID)
    {
        return Lobbies.FirstOrDefault(l => l.LobbyGUID == lobbyGUID);
    }
    public static void JoinLobby(string lobbyGUID, string playerName)
    {
        if (IsPlayerInLobby(playerName))
            throw new InvalidOperationException("Player is already in a lobby.");

        var lobby = GetLobby(lobbyGUID);
        if (lobby != null && lobby.Players.Count < lobby.MaxPlayers && !lobby.Players.Contains(playerName))
        {
            lobby.Players.Add(playerName);
            lobby.PlayerReadiness[playerName] = false;
            lobby.Settings.OrderOfPlayerNames.Add(playerName);
        }
        
    }
    public static bool LeaveLobby(string lobbyGUID, string playerName, out bool isCreator)
    {
        isCreator = false;
        var lobby = GetLobby(lobbyGUID);
        if (lobby != null && lobby.Players.Contains(playerName))
        {
            if (lobby.Creator == playerName)
            {
                isCreator = true;
                return true;
            }

            lobby.Players.Remove(playerName);
            lobby.PlayerReadiness.Remove(playerName);
            lobby.Settings.OrderOfPlayerNames.Remove(playerName);
        }

        return false;
    }

    public static void DeleteLobby(string lobbyGUID)
    {
        var lobby = GetLobby(lobbyGUID);
        if (lobby != null)
        {
            Lobbies.Remove(lobby);
        }
    }
    public static void CloseLobby(string lobbyGUID)
    {
        var lobby = GetLobby(lobbyGUID);
        if (lobby == null) return;
        lobby.Status = LobbyStatus.Finished;
        Lobbies.Remove(lobby);
    }

    public static void RemovePlayerFromLobby(string lobbyGUID, string playerName)
    {
        var lobby = GetLobby(lobbyGUID);
        if (lobby == null) return;

        lobby.Players.Remove(playerName);
        lobby.PlayerReadiness.Remove(playerName);
        lobby.Settings?.OrderOfPlayerNames?.Remove(playerName);
    }


        private static bool IsPlayerInLobby(string username)
    {
        return Lobbies.Any(l => l.Players.Contains(username));
    }
}