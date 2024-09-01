namespace AutoBlumFarmServer.DTO
{
    public class UserDTO
    {
        public int     Id                  { get; set; }
        public long    TelegramUserId      { get; set; }
        public string  FirstName           { get; set; } = string.Empty;
        public string  LastName            { get; set; } = string.Empty;
        public decimal BalanceUSD          { get; set; }
        public string  LanguageCode        { get; set; } = "en";
        public string  OwnReferralCode     { get; set; } = string.Empty;
        public string  PhotoUrl            { get; set; } = string.Empty;
        public double  AccountsBalancesSum { get; set; }
        public bool    HadTrial            { get; set; }
    }
}
