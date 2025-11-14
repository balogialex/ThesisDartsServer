using DartsAPI.Data;
using DartsAPI.Dtos;
using DartsAPI.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using DartsAPI.Hubs;
using DartsAPI.Services;

namespace DartsAPI.Endpoints;

public static class LobbyEndpoints
{
    public static RouteGroupBuilder MapLobbyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("lobbies").RequireAuthorization();


        group.MapPost("/", (CreateLobbyDto createDto, PlayerContext dbContext, HttpContext httpContext, IHubContext<LobbyHub> hubContext) =>
        {
            var username = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? throw new UnauthorizedAccessException("Username not found in token.");

            var player = dbContext.Players.FirstOrDefault(p => p.Username == username);
            if (player == null || player.Username != createDto.LobbyCreator) 
                return Results.Problem(
                    detail: "Player not found or creator mismatch.",
                    statusCode: StatusCodes.Status401Unauthorized);

            try
            {
                var lobby = LobbyManager.CreateLobby(createDto);
                var lobbyDto = new LobbyDto
                {
                    LobbyGUID = lobby.LobbyGUID,
                    LobbyCreator = lobby.Creator,
                    LobbyTitle = lobby.Title,
                    Settings = lobby.Settings,
                    PlayerReadiness = lobby.PlayerReadiness,
                    Players = lobby.Players
                };

                hubContext.Clients.All.SendAsync("LobbyCreated", lobbyDto);

                return Results.Ok(lobbyDto);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/", () =>
        {
            var lobbies = LobbyManager.GetAllLobbies();
            var lobbyDtos = lobbies.Select(l => new LobbyDto
            {
                LobbyGUID = l.LobbyGUID,
                LobbyCreator = l.Creator,
                LobbyTitle = l.Title,
                Settings = l.Settings
            }).ToList();

            return Results.Ok(lobbyDtos);
        });

        group.MapPost("/{lobbyGUID}/join", (string lobbyGUID, PlayerContext dbContext, HttpContext httpContext, IHubContext<LobbyHub> hubContext) =>
        {
            var username = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? throw new UnauthorizedAccessException("Username not found in token.");

            var player = dbContext.Players.FirstOrDefault(p => p.Username == username);
            if (player == null) 
                return Results.Problem(
                    detail: "Player not found.",
                    statusCode: StatusCodes.Status401Unauthorized);

            var lobby = LobbyManager.GetLobby(lobbyGUID);
            if (lobby == null) return Results.NotFound("Lobby not found.");
            if (lobby.Status != LobbyStatus.Created) return Results.BadRequest("Lobby is not open.");
            if (lobby.Players.Count >= lobby.MaxPlayers) return Results.BadRequest("Lobby is full.");
            if (lobby.Players.Contains(player.Username)) return Results.BadRequest("Player is already in this lobby.");

            LobbyManager.JoinLobby(lobbyGUID, player.Username);
            var updatedLobby = LobbyManager.GetLobby(lobbyGUID);
            var lobbyDto = new LobbyDto
            {
                LobbyGUID = updatedLobby.LobbyGUID,
                LobbyCreator = updatedLobby.Creator,
                LobbyTitle = updatedLobby.Title,
                Settings = updatedLobby.Settings,
                Players = updatedLobby.Players,
                PlayerReadiness = updatedLobby.PlayerReadiness
            };

            hubContext.Clients.Group(lobbyGUID).SendAsync("PlayerJoined", lobbyDto);
            return Results.Ok(lobbyDto);
        });

        group.MapPost("/{lobbyGUID}/leave", async (string lobbyGUID, PlayerContext dbContext, HttpContext httpContext, IHubContext<LobbyHub> hubContext) =>
        {
            var username = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? throw new UnauthorizedAccessException("Username not found in token.");

            var player = dbContext.Players.FirstOrDefault(p => p.Username == username);
            if (player == null) return Results.Problem(detail: "Player not found.", statusCode: StatusCodes.Status401Unauthorized);

            var lobby = LobbyManager.GetLobby(lobbyGUID);
            if (lobby == null) return Results.NotFound("Lobby not found.");
            if (!lobby.Players.Contains(username)) return Results.BadRequest("Player is not in this lobby.");

            if (lobby.Status == LobbyStatus.Started)
            {
                try {GameStateStore.Forfeit(lobbyGUID, username); } catch {  }
                LobbyManager.CloseLobby(lobbyGUID);
                GameStateStore.Cleanup(lobbyGUID);
                await hubContext.Clients.Group(lobbyGUID).SendAsync("LobbyClosed", new LobbyDto { LobbyGUID = lobbyGUID });
                return Results.Ok("Match ended due to player leaving.");
            }

            bool isCreator;
            bool leftIsCreator = LobbyManager.LeaveLobby(lobbyGUID, username, out isCreator);

            if (leftIsCreator)
            {
                await hubContext.Clients.Group(lobbyGUID).SendAsync("LobbyDeleted", new { LobbyGUID = lobbyGUID });
                await hubContext.Clients.All.SendAsync("LobbyClosed", new { LobbyGUID = lobbyGUID });

                LobbyManager.DeleteLobby(lobbyGUID);
                return Results.Ok("Lobby deleted because creator left.");
            }

            lobby = LobbyManager.GetLobby(lobbyGUID);
            if (lobby != null)
            {
                var lobbyDto = new LobbyDto
                {
                    LobbyGUID = lobby.LobbyGUID,
                    LobbyCreator = lobby.Creator,
                    LobbyTitle = lobby.Title,
                    Settings = lobby.Settings,
                    Players = lobby.Players,
                    PlayerReadiness = lobby.PlayerReadiness
                };

                await hubContext.Clients.Group(lobbyGUID).SendAsync("PlayerLeft", lobbyDto);
            }

            return Results.Ok("Successfully left the lobby.");
        });

        group.MapGet("/{lobbyGUID}/status", (string lobbyGUID, IHubContext<LobbyHub> hubContext) =>
        {
            var lobby = LobbyManager.GetLobby(lobbyGUID);
            if (lobby == null) return Results.NotFound("Lobby not found.");
            var lobbyDto = new LobbyDto
            {
                LobbyGUID = lobby.LobbyGUID,
                LobbyCreator = lobby.Creator,
                LobbyTitle = lobby.Title,
                Settings = lobby.Settings,
                Players = lobby.Players,
                PlayerReadiness = lobby.PlayerReadiness
            };
            return Results.Ok(lobbyDto);
        });

        group.MapPost("/{lobbyGUID}/ready", (string lobbyGUID, HttpContext httpContext, IHubContext<LobbyHub> hubContext) =>
        {
            var username = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? throw new UnauthorizedAccessException("Username not found in token.");

            var lobby = LobbyManager.GetLobby(lobbyGUID);
            if (lobby == null) return Results.NotFound("Lobby not found.");
            if (!lobby.Players.Contains(username)) return Results.BadRequest("Player is not in this lobby.");

            lobby.PlayerReadiness[username] = !lobby.PlayerReadiness.GetValueOrDefault(username, false);
            var updatedLobby = LobbyManager.GetLobby(lobbyGUID); // Frissítjük a lobby állapotát
            var lobbyDto = new LobbyDto
            {
                LobbyGUID = updatedLobby.LobbyGUID,
                LobbyCreator = updatedLobby.Creator,
                LobbyTitle = updatedLobby.Title,
                Settings = updatedLobby.Settings,
                Players = updatedLobby.Players,
                PlayerReadiness = updatedLobby.PlayerReadiness
            };

            hubContext.Clients.Group(lobbyGUID).SendAsync("ReadyStatusUpdated", lobbyDto);

            return Results.Ok("Ready status updated.");
        });

        group.MapPost("/{lobbyGUID}/start", (string lobbyGUID, HttpContext httpContext, IHubContext<LobbyHub> hubContext) =>
        {
            var username = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? throw new UnauthorizedAccessException("Username not found in token.");

            var lobby = LobbyManager.GetLobby(lobbyGUID);
            if (lobby == null) return Results.NotFound("Lobby not found.");
            if (lobby.Creator != username) return Results.BadRequest("Only the creator can start the game.");
            if (lobby.Players.Any(p => !lobby.PlayerReadiness.GetValueOrDefault(p, false)))
                return Results.BadRequest("Not all players are ready.");

            lobby.Status = LobbyStatus.Started;

            GameStateStore.InitializeFromLobby(lobbyGUID, lobby.Players, lobby.Settings);

            var lobbyDto = new LobbyDto
            {
                LobbyGUID = lobby.LobbyGUID,
                LobbyCreator = lobby.Creator,
                LobbyTitle = lobby.Title,
                Settings = lobby.Settings,
                Players = lobby.Players,
                PlayerReadiness = lobby.PlayerReadiness
            };

            hubContext.Clients.Group(lobbyGUID).SendAsync("GameStarted", lobbyDto);

            return Results.Ok("Game started.");
        });


        return group;
    }
}