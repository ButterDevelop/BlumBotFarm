using BlumBotFarm.Core;
using Serilog;
using Telegram.Bot;
using TaskScheduler = BlumBotFarm.Scheduler.TaskScheduler;

namespace BlumBotFarm.Startup
{
    class Program
    {
        private static readonly string MASK_DATE_LOG_FILE_PATH = "%DateTime%", LOG_FILE_PATH = $"logs/blumBotFarm-{MASK_DATE_LOG_FILE_PATH}.log";

        internal static string GetLogFilePath()
        {
            return LOG_FILE_PATH.Replace(MASK_DATE_LOG_FILE_PATH, DateTime.Now.ToString("yyyyMMddHH"));
        }

        static async Task Main(string[] args)
        {
            Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture   = System.Globalization.CultureInfo.GetCultureInfo("en-US");

            Log.Logger = new LoggerConfiguration()
                         .MinimumLevel.Debug()
                         .WriteTo.Console()
                         .WriteTo.File(LOG_FILE_PATH.Replace(MASK_DATE_LOG_FILE_PATH, ""), rollingInterval: RollingInterval.Hour)
                         .CreateLogger();

            // Инициализация базы данных
            Database.Database.Initialize();

            // Инициализация UserAgents
            HTTPController.Initialize(Properties.Resources.AndroidBoughtUserAgents);

            // Настройка Telegram-бота через конфигурацию
            var userBotToken   = AppConfig.BotSettings.BotToken;
            var adminBotToken  = AppConfig.BotSettings.AdminBotToken;
            var adminUsernames = AppConfig.BotSettings.AdminUsernames;
            var adminChatIds   = AppConfig.BotSettings.AdminChatIds;
            if (adminBotToken != null && userBotToken != null && adminUsernames != null && adminChatIds != null)
            {
                var adminBotClient = new TelegramBotClient(adminBotToken);
                var userBotClient  = new TelegramBotClient(userBotToken);

                var adminTelegramBot = new TelegramBot.AdminTelegramBot(adminBotClient, adminUsernames, adminChatIds);
                adminTelegramBot.Start();

                var messageProcessor = new MessageProcessor.MessageProcessor(adminBotClient, userBotClient, adminUsernames, adminChatIds, new CancellationToken());
                await Task.Factory.StartNew(messageProcessor.StartAsync);

                Log.Information("Started Admin Telegram bot and Message processor.");
            }

            // Starting Auto Account Starter
            var autoAccountStarter = new AutoAccountStarter.AutoAccountStarter(new CancellationToken());
            await Task.Factory.StartNew(autoAccountStarter.StartAsync);
            Log.Information("Started Auto Account Starter.");

            // Main Job execute
            await TaskScheduler.ExecuteMainJobNow();
            Log.Information("Started Main Scheduler Job.");

            // Infinite loop for proper work
            DateTime start = DateTime.Now;
            Log.Information("Started an infinite loop.");
            while (true)
            {
                Log.Information($"Infinite loop: I am working. Uptime: {DateTime.Now - start:hh\\:mm\\:ss}");

                Thread.Sleep(60 * 1000); // Wait 1 minute
            }
        }
    }
}
