namespace BlumBotFarm.Core.Models
{
    public class StarsPayment
    {
        public int      Id                { get; set; }
        public int      UserId            { get; set; }
        public decimal  AmountUsd         { get; set; }
        public int      AmountStars       { get; set; }
        public DateTime CreatedDateTime   { get; set; }
        public bool     IsCompleted       { get; set; }
        public DateTime CompletedDateTime { get; set; }
    }
}
