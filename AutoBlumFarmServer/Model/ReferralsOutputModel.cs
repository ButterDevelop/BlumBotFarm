namespace AutoBlumFarmServer.Model
{
    public class ReferralsOutputModel
    {
        public string  FirstName    { get; set; } = string.Empty;
        public string  LastName     { get; set; } = string.Empty;
        public decimal HostEarnings { get; set; }
        public string  PhotoUrl     { get; set; } = string.Empty;
    }
}
