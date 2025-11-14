namespace DartsAPI.Dtos
{
    public class ThrowAppliedDto
    {
        public string LobbyGUID { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public int Scored { get; set; }
        public int NewScore { get; set; }
        public bool LegEnded { get; set; }
        public bool MatchEnded { get; set; }
        public int DoubleTries { get; set; }
        public int UsedDarts { get; set; }
        public string ClientThrowId { get; set; } = string.Empty;
    }
}
