namespace BlumBotFarm.Core.Models
{
    public class Account
    {
        public int      Id                    { get; set; }
        public int      UserId                { get; set; }
        public string   CustomUsername        { get; set; } = string.Empty;
        public string   BlumUsername          { get; set; } = string.Empty;
        public string   BlumAccountId         { get; set; } = string.Empty;
        public double   Balance               { get; set; } = 0;
        public int      Tickets               { get; set; } = 0;
        public int      ReferralsCount        { get; set; } = 0;
        public string   ReferralLink          { get; set; } = string.Empty;
        public string   AccessToken           { get; set; } = string.Empty;
        public string   RefreshToken          { get; set; } = string.Empty;
        public string   ProviderToken         { get; set; } = string.Empty;
        public string   UserAgent             { get; set; } = string.Empty;
        public string   Proxy                 { get; set; } = string.Empty;
        public string   CountryCode           { get; set; } = string.Empty;
        public int      ProxySellerListId     { get; set; } = 0;
        public int      TimezoneOffset        { get; set; } = -120;
        public string   LastStatus            { get; set; } = string.Empty;
        public bool     IsTrial               { get; set; } = false;
        public DateTime TrialExpires          { get; set; } = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public bool     IsEligibleForDogsDrop { get; set; } = false;
    }
}
