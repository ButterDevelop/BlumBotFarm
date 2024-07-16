namespace BlumBotFarm.Core.Models
{
    public class User
    {
        public int      Id              { get; set; }
        public long     TelegramUserId  { get; set; }
        public string   FirstName       { get; set; } = string.Empty;
        public string   LastName        { get; set; } = string.Empty;
        public decimal  BalanceUSD      { get; set; }
        public bool     IsBanned        { get; set; }
        public string   LanguageCode    { get; set; } = "en";
        public string   OwnReferralCode { get; set; } = string.Empty;
        public DateTime CreatedAt       { get; set; }
    }
}
