using Microsoft.Extensions.Configuration;

namespace BlumBotFarm.Core
{
    public static class AppConfig
    {
        public static IConfiguration Configuration { get; }

        static AppConfig()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            Configuration = builder.Build();
        }

        public static class BotSettings
        {
            public static string? Token => Configuration["BotSettings:Token"];
            public static string[]? AdminUsernames => Configuration.GetSection("BotSettings:AdminUsernames").Get<string[]>();
        }

        public static class DatabaseSettings
        {
            public static string? ConnectionString => Configuration["DatabaseSettings:ConnectionString"];
        }
    }
}
