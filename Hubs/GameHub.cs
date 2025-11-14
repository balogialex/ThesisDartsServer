using Microsoft.AspNetCore.SignalR;

namespace DartsAPI.Hubs
{
    public class GameHub : Hub
    {
        public Task JoinGameGroup(string lobbyGUID) =>
            Groups.AddToGroupAsync(Context.ConnectionId, lobbyGUID);

        public Task LeaveGameGroup(string lobbyGUID) =>
            Groups.RemoveFromGroupAsync(Context.ConnectionId, lobbyGUID);
    }
}
