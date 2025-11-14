namespace DartsAPI.Dtos
{
    public class GameSummaryDto
    {
        public string LobbyGUID { get; set; } = string.Empty;
        public string Winner { get; set; } = string.Empty;

        public string EndReason { get; set; } = "Normal";
        public string? ForfeitedBy { get; set; }

        public Dictionary<string, int> LegsWon { get; set; } = new();
        public Dictionary<string, int> TotalPoints { get; set; } = new();
        public Dictionary<string, int> TotalThrows { get; set; } = new();
        public Dictionary<string, double> Average { get; set; } = new();
        public Dictionary<string, int> CheckoutAttempts { get; set; } = new();
        public Dictionary<string, int> Checkouts { get; set; } = new();
        public Dictionary<string, int> CheckoutPercent { get; set; } = new();
    }
}
