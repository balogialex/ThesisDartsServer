using DartsAPI.Dtos;
using DartsAPI.Hubs;
using DartsAPI.Models;
using DartsAPI.Services;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace DartsAPI.Endpoints
{
    public static class GameEndpoints
    {
        public static RouteGroupBuilder MapGameEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("games").RequireAuthorization();

            group.MapPost("/{lobbyGUID}/throw", async (
                string lobbyGUID,
                SubmitThrowDto dto,
                HttpContext http,
                IHubContext<GameHub> hub,
                IHubContext<LobbyHub> lobbyHub) =>
            {
                var username = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? throw new UnauthorizedAccessException("Username not found in token.");

                var applied = GameStateStore.ApplyThrow(lobbyGUID, dto, username);

                await hub.Clients.Group(lobbyGUID).SendAsync("ThrowApplied", applied);

                if (applied.LegEnded || applied.MatchEnded)
                {
                    var snapshot = GameStateStore.GetSnapshot(lobbyGUID);
                    await hub.Clients.Group(lobbyGUID).SendAsync("FullState", snapshot);
                }

                if (applied.MatchEnded)
                {
                    var summary = GameStateStore.BuildSummary(lobbyGUID);
                    await hub.Clients.Group(lobbyGUID).SendAsync("MatchEnded", summary);

                    LobbyManager.CloseLobby(lobbyGUID);
                    GameStateStore.Cleanup(lobbyGUID);
                }

                return Results.Ok(applied);
            });

            group.MapGet("/{lobbyGUID}/state", (string lobbyGUID) =>
            {
                var snapshot = GameStateStore.GetSnapshot(lobbyGUID);
                return Results.Ok(snapshot);
            });

            group.MapPost("/{lobbyGUID}/forfeit", async (
                string lobbyGUID,
                HttpContext http,
                IHubContext<GameHub> hub,
                IHubContext<LobbyHub> lobbyHub) =>
            {
                var username = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? throw new UnauthorizedAccessException("Username not found in token.");

                var applied = GameStateStore.Forfeit(lobbyGUID, username);

                await hub.Clients.Group(lobbyGUID).SendAsync("ThrowApplied", applied);

                var snapshot = GameStateStore.GetSnapshot(lobbyGUID);
                await hub.Clients.Group(lobbyGUID).SendAsync("FullState", snapshot);

                var summary = GameStateStore.BuildSummary(lobbyGUID);
                await hub.Clients.Group(lobbyGUID).SendAsync("MatchEnded", summary);

                LobbyManager.CloseLobby(lobbyGUID);
                GameStateStore.Cleanup(lobbyGUID);

                return Results.Ok(applied);
            });

            return group;
        }
    }
}
