using System;
using DartsAPI.Models;

namespace DartsAPI.Dtos;

public class LobbyDto
{
    
        public string LobbyGUID { get; set; } = string.Empty;
        public string LobbyCreator { get; set; } = string.Empty;
        public string LobbyTitle { get; set; } = string.Empty;
        public GameSettings Settings { get; set; }
        public List<string> Players { get; set; } = new List<string>();
        public Dictionary<string, bool> PlayerReadiness { get; set; } = new Dictionary<string, bool>();


}
