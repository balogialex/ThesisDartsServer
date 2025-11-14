using System;
using Microsoft.AspNetCore.SignalR;

namespace DartsAPI.Hubs;

public class LobbyHub : Hub
{
    //Kliens belépése egy lobby csoportba
    public async Task JoinLobbyGroup(string lobbyGUID)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, lobbyGUID);
        Console.WriteLine($"Client {Context.ConnectionId} joined group {lobbyGUID}");
    }

    // Kliens kilépése egy lobby csoportból
    public async Task LeaveLobbyGroup(string lobbyGUID)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobbyGUID);
        Console.WriteLine($"Client {Context.ConnectionId} left group {lobbyGUID}");
    }
}
