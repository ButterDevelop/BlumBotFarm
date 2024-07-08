namespace BlumBotFarm.Core.Models
{
    public class User
    {
        public int     Id             { get; set; }
        public long    TelegramUserId { get; set; }
        public decimal BalanceUSD     { get; set; }
        public bool    IsBanned       { get; set; }
    }
}
