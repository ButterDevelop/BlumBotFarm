namespace BlumBotFarm.Core.Models
{
    public class Message
    {
        public int      Id          { get; set; }
        public long     ChatId      { get; set; }
        public string   MessageText { get; set; } = string.Empty;
        public DateTime CreatedAt   { get; set; }
        public bool     IsSilent    { get; set; }
    }
}
