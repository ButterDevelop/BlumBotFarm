namespace AutoBlumFarmServer
{
    public class Config
    {
        private static Config  _instance = new();
        private IConfiguration _configuration;

        public Config()
        {
            // Билдер конфигураций
            var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.Api.json", optional: false, reloadOnChange: true);
            _configuration = builder.Build();
        }

        public static Config Instance 
        { 
            get { return _instance; }
            set { _instance = value; }
        }

        // COMMON PART
        public string SERVER_DOMAIN                  => _configuration["ServerDomain"] ?? "";
        public int    REFERRAL_BALANCE_BONUS_PERCENT => _configuration.GetValue("Referral:BalanceBonusPercent", 10);

        // JWT PART
        public string JWT_ISSUER            => _configuration["Jwt:Issuer"]   ?? "";
        public string JWT_AUDIENCE          => _configuration["Jwt:Audience"] ?? "";
        public string JWT_KEY               => _configuration["Jwt:Key"]      ?? "";
        public int    JWT_LIVE_HOURS        => _configuration.GetValue("Jwt:LiveHours", 8);

        // TELEGRAM PART
        public long   TELEGRAM_TECH_SUPPORT_GROUP_CHAT_ID => _configuration.GetValue<long>("Telegram:TechSupportGroupChatId", 0);
        public string TELEGRAM_PUBLIC_BOT_NAME            => _configuration["Telegram:PublicBotName"] ?? "";
        public string TELEGRAM_BOT_TOKEN                  => _configuration["Telegram:BotToken"] ?? "";
        public string TELEGRAM_ADMIN_BOT_TOKEN            => _configuration["Telegram:AdminBotToken"] ?? "";
        public string TELEGRAM_WALLET_TOKEN               => _configuration["Telegram:WalletToken"] ?? "";

        // TELEGRAM STARS
        public string  TG_STARS_PAYMENT_TITLE             => _configuration["TelegramStarsPayment:Title"] ?? "";
        public string  TG_STARS_PAYMENT_DESCRIPTION       => _configuration["TelegramStarsPayment:Description"] ?? "";
        public string  TG_STARS_PAYMENT_PRICE_LABEL       => _configuration["TelegramStarsPayment:PriceLabel"] ?? "";
        public string  TG_STARS_PAYMENT_INVOICE_PHOTO_URL => _configuration["TelegramStarsPayment:InvoicePhotoUrl"] ?? "";
        public double  TG_STARS_PAYMENT_STAR_USD_PRICE    => _configuration.GetValue("TelegramStarsPayment:StarUsdPrice", 2.24);

        // SQL PART                         
        public string SQL_CONNECTION_STRING => _configuration["DatabaseSettings:ConnectionString"] ?? "Data Source=blumbotfarm.db";
    }
}
