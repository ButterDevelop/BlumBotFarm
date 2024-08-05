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
            public static string? BotToken      => Configuration["BotSettings:BotToken"];
            public static string? AdminBotToken => Configuration["BotSettings:AdminBotToken"];
            public static string? PublicBotName => Configuration["BotSettings:PublicBotName"];
            public static long[]? AdminChatIds  => Configuration.GetSection("BotSettings:AdminChatIds").Get<long[]>();
        }

        public static class DatabaseSettings
        {
            // SQL PART
            public static string SQL_CONNECTION_STRING       => Configuration["DatabaseSettings:ConnectionStrings:SqlConnectionString"] ?? "Data Source=blumbotfarmDefault.db";

            // MONGO PART
            public static string MONGO_CONNECTION_STRING     => Configuration["DatabaseSettings:ConnectionStrings:MongoConnectionString"] ?? "";
            public static string MONGO_DATABASE_NAME         => Configuration["DatabaseSettings:ConnectionStrings:MongoDatabaseName"] ?? "";

            // MONGO TABLES
            public static string MONGO_ACCOUNT_PATH          => Configuration["DatabaseSettings:MongoTableNames:MONGO_ACCOUNT_PATH"] ?? "";
            public static string MONGO_TASK_PATH             => Configuration["DatabaseSettings:MongoTableNames:MONGO_TASK_PATH"] ?? "";
            public static string MONGO_MESSAGE_PATH          => Configuration["DatabaseSettings:MongoTableNames:MONGO_MESSAGE_PATH"] ?? "";
            public static string MONGO_EARNING_PATH          => Configuration["DatabaseSettings:MongoTableNames:MONGO_EARNING_PATH"] ?? "";
            public static string MONGO_REFERRAL_PATH         => Configuration["DatabaseSettings:MongoTableNames:MONGO_REFERRAL_PATH"] ?? "";
            public static string MONGO_USER_PATH             => Configuration["DatabaseSettings:MongoTableNames:MONGO_USER_PATH"] ?? "";
            public static string MONGO_WALLET_PAYMENT_PATH   => Configuration["DatabaseSettings:MongoTableNames:MONGO_WALLET_PAYMENT_PATH"] ?? "";
            public static string MONGO_DAILY_REWARDS_PATH    => Configuration["DatabaseSettings:MongoTableNames:MONGO_DAILY_REWARDS_PATH"] ?? "";
            public static string MONGO_STARS_PAYMENT_PATH    => Configuration["DatabaseSettings:MongoTableNames:MONGO_STARS_PAYMENT_PATH"] ?? "";
            public static string MONGO_FEEDBACK_MESSAGE_PATH => Configuration["DatabaseSettings:MongoTableNames:MONGO_FEEDBACK_MESSAGE_PATH"] ?? "";
        }
    }
}
