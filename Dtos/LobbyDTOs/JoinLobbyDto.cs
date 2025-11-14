using System;

namespace DartsAPI.Dtos;

public class JoinLobbyDto
{
        public string LobbyGUID { get; set; } = string.Empty;
        public string NewPlayerName { get; set; } = string.Empty;
}
