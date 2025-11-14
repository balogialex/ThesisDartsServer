using System;

namespace DartsAPI.Models;

public class Lobby
{
    public string LobbyGUID { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Creator { get; set; } = string.Empty;
    public int MaxPlayers { get; set; } = 2;
    public LobbyStatus Status { get; set; } = LobbyStatus.Created;
    public GameSettings Settings { get; set; } = new GameSettings();
    public List<string> Players { get; set; } = new List<string>();
    public Dictionary<string, bool> PlayerReadiness { get; set; } = new Dictionary<string, bool>(); // Új mező

}

public enum LobbyStatus
{
    Created,
    Started,
    Finished
}