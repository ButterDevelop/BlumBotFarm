namespace AutoBlumFarmServer.Model
{
    public class ReferralsOutputModel
    {
        public int     Id           { get; set; }
        public string  FirstName    { get; set; } = string.Empty;
        public string  LastName     { get; set; } = string.Empty;
        public decimal HostEarnings { get; set; }
    }
}
