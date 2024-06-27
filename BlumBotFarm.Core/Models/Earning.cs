namespace BlumBotFarm.Core.Models
{
    public class Earning
    {
        public int      Id        { get; set; }
        public int      AccountId { get; set; }
        public double   Total     { get; set; }
        public DateTime Created   { get; set; }
        public string   Action    { get; set; } = string.Empty;
    }
}
