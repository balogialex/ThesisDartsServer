using System.Collections.Generic;

namespace DartsAPI.Dtos
{
    public class GameStateDto
    {
        public string LobbyGUID { get; set; } = string.Empty;
        public string CurrentPlayer { get; set; } = string.Empty;
        public Dictionary<string, int> Scores { get; set; } = new();
        public bool LegEnded { get; set; }
        public bool MatchEnded { get; set; }
    }
}
