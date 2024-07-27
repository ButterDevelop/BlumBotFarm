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
                .AddJsonFile("appsettings.Startup.json", optional: false, reloadOnChange: true);
            Configuration = builder.Build();
        }

        public static class BotSettings
        {
            public static string?   BotToken       => Configuration["BotSettings:BotToken"];
            public static string?   AdminBotToken  => Configuration["BotSettings:AdminBotToken"];
            public static string?   PublicBotName  => Configuration["BotSettings:PublicBotName"];
            public static long[]?   AdminChatIds   => Configuration.GetSection("BotSettings:AdminChatIds").Get<long[]>();
        }

        public static class DatabaseSettings
        {
            public static string? ConnectionString => Configuration["DatabaseSettings:ConnectionString"];
        }
    }
}
