using System.Reflection;

namespace AutoBlumFarmServer
{
    public class Config
    {
        private static Config  _instance = new();
        private IConfiguration _configuration;

        public Config()
        {
            // Билдер конфигураций
            var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", optional: false, reloadOnChange: true);
            _configuration = builder.Build();
        }

        public static Config Instance 
        { 
            get { return _instance; }
            set { _instance = value; }
        }

        // JWT PART
        public string JWT_ISSUER            => _configuration["Jwt:Issuer"]   ?? "";
        public string JWT_AUDIENCE          => _configuration["Jwt:Audience"] ?? "";
        public string JWT_KEY               => _configuration["Jwt:Key"]      ?? "";
        
        // TELEGRAM PART
        public string TELEGRAM_BOT_TOKEN    => _configuration["Telegram:BotToken"] ?? "";

        // SQL PART                         
        public string SQL_CONNECTION_STRING => _configuration["DatabaseSettings:ConnectionString"] ?? "Data Source=blumbotfarm.db";
    }
}
