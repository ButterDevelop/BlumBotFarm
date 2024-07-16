namespace AutoBlumFarmServer.DTO
{
    public class AccountDTO
    {
        public int    Id              { get; set; }
        public string CustomUsername  { get; set; } = string.Empty;
        public string BlumUsername    { get; set; } = string.Empty;
        public double Balance         { get; set; }
        public int    Tickets         { get; set; }
        public int    ReferralCount   { get; set; }
        public string ReferralLink    { get; set; } = string.Empty;
        public string BlumAuthData    { get; set; } = string.Empty;
        public double EarnedToday     { get; set; }
        public bool   TookDailyReward { get; set; }
        public string NearestWorkIn   { get; set; } = string.Empty;
        public string CountryCode     { get; set; } = string.Empty;
        public string LastStatus      { get; set; } = string.Empty;
    }
}
