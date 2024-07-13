namespace AutoBlumFarmServer.DTO
{
    public class AccountDTO
    {
        public int    Id              { get; set; }
        public string Username        { get; set; } = string.Empty;
        public double Balance         { get; set; }
        public int    Tickets         { get; set; }
        public int    ReferralCount   { get; set; }
        public string ReferralLink    { get; set; } = string.Empty;
        public string BlumAuthData    { get; set; } = string.Empty;
        public double EarnedToday     { get; set; }
        public bool   TookDailyReward { get; set; }
    }
}
