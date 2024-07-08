namespace AutoBlumFarmServer.DTO
{
    public class AccountDTO
    {
        public int    Id           { get; set; }
        public string Username     { get; set; } = string.Empty;
        public double Balance      { get; set; }
        public int    Tickets      { get; set; }
        public string BlumAuthData { get; set; } = string.Empty;
    }
}
