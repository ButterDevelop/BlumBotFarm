namespace BlumBotFarm.Core.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string ChatId { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
