namespace BlumBotFarm.Core.Models
{
    public class Account
    {
        public int    Id             { get; set; }
        public string Username       { get; set; } = string.Empty;
        public double Balance        { get; set; }
        public int    Tickets        { get; set; }
        public string AccessToken    { get; set; } = string.Empty;
        public string RefreshToken   { get; set; } = string.Empty;
        public string ProviderToken  { get; set; } = string.Empty;
        public string UserAgent      { get; set; } = string.Empty;
        public string Proxy          { get; set; } = string.Empty;
        public int    TimezoneOffset { get; set; }
    }
}
