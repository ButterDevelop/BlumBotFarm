namespace AutoBlumFarmServer.DTO
{
    public class UserDTO
    {
        public int     Id              { get; set; }
        public long    TelegramUserId  { get; set; }
        public decimal BalanceUSD      { get; set; }
        public string  LanguageCode    { get; set; } = "en";
        public string  OwnReferralCode { get; set; } = string.Empty;
    }
}
