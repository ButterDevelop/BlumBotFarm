using CsvHelper.Configuration.Attributes;

namespace BlumBotFarm.GUIAccountManager
{
    public class CsvRecord
    {
        [Name("Number")]
        public int? Number { get; set; }

        [Name("Account Name")]
        public string AccountName { get; set; } = string.Empty;

        [Name("Telegram Name")]
        public string TelegramName { get; set; } = string.Empty;

        [Name("Proxy")]
        public string Proxy { get; set; } = string.Empty;

        [Name("Access Token")]
        public string AccessToken { get; set; } = string.Empty;

        [Name("Refresh Token")]
        public string RefreshToken { get; set; } = string.Empty;

        [Name("Auth TG Query Link")]
        public string AuthTGQueryLink { get; set; } = string.Empty;

        [Name(" Refferal Link")]
        public string ReferralLink { get; set; } = string.Empty;

        [Name("Telegram Add Command")]
        public string TelegramAddCommand { get; set; } = string.Empty;
    }
}
