using System;
using DartsAPI.Models;

namespace DartsAPI.Dtos;

public class CreateLobbyDto
{
        public string LobbyTitle { get; set; } = string.Empty;
        public string LobbyCreator { get; set; } = string.Empty;
        public GameSettings Settings { get; set; }
}
