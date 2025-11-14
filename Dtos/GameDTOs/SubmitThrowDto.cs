namespace DartsAPI.Dtos
{
    public class SubmitThrowDto
    {
        public string PlayerName { get; set; } = string.Empty;
        public int InputScore { get; set; }
        public int DoubleTries { get; set; }
        public int UsedDarts { get; set; }
        public string ClientThrowId { get; set; } = Guid.NewGuid().ToString();
    }
}