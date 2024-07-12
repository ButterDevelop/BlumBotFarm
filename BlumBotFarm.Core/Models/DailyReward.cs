namespace BlumBotFarm.Core.Models
{
    public class DailyReward
    {
        public int      Id              { get; set; }
        public long     AccountId       { get; set; }
        public DateTime CreatedAt       { get; set; }
    }
}
