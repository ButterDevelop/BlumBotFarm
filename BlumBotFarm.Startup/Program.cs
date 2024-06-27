using BlumBotFarm.Core;
using BlumBotFarm.Database.Repositories;
using BlumBotFarm.GameClient;
using BlumBotFarm.Scheduler.Jobs;
using Quartz;
using Serilog;
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
            HTTPController.Initialize(Properties.Resources.UserAgents);

            // Настройка Telegram-бота через конфигурацию
            var botToken       = AppConfig.BotSettings.Token;
            var adminUsernames = AppConfig.BotSettings.AdminUsernames;
            var adminChatIds   = AppConfig.BotSettings.AdminChatIds;
            if (botToken != null && adminUsernames != null && adminChatIds != null)
            {
                var telegramBot = new TelegramBot.TelegramBot(botToken, adminUsernames, adminChatIds);
                telegramBot.Start();

                var messageProcessor = new MessageProcessor.MessageProcessor(botToken, adminUsernames, adminChatIds, new CancellationToken());
                await Task.Factory.StartNew(messageProcessor.StartAsync);

                Log.Information("Started Telegram bot and Message processor.");
            }

            // Создание экземпляра планировщика задач
            var taskScheduler = new TaskScheduler();

            using (var db = Database.Database.GetConnection())
            {
                var accountRepo  = new AccountRepository(db);
                var taskRepo     = new TaskRepository(db);

                // Получение всех аккаунтов из базы данных
                var accounts = accountRepo.GetAll();

                foreach (var account in accounts)
                {
                    // Восстановление задач из базы данных
                    var tasks = taskRepo.GetAll().Where(t => t.AccountId == account.Id);

                    foreach (var task in tasks)
                    {
                        // Если задача еще актуальна
                        if (task.NextRunTime > DateTime.Now)
                        {
                            IJobDetail job = task.TaskType == "DailyCheckJob" 
                                             ? JobBuilder.Create<DailyCheckJob>().Build() : JobBuilder.Create<FarmingJob>().Build();
                            job.JobDataMap.Put("accountId",              account.Id);
                            job.JobDataMap.Put("taskId" + task.TaskType, task.Id);
                            job.JobDataMap.Put("isPlanned", true);

                            var trigger = TriggerBuilder.Create()
                                .WithSimpleSchedule(schedule => schedule
                                    .WithIntervalInSeconds(task.ScheduleSeconds)
                                    .RepeatForever())
                                .StartAt(task.NextRunTime)
                                .Build();

                            Log.Information($"Schedule task - accountId: {account.Id}, accountUsername: {account.Username}, taskId: {task.Id}, taskType: {task.TaskType}");

                            await taskScheduler.ScheduleTask(task.Id.ToString(), task.Id.ToString(), job, trigger);
                        }
                        else
                        {
                            Log.Information($"Reschedule missed task - accountId: {account.Id}, accountUsername: {account.Username}, taskId: {task.Id}, taskType: {task.TaskType}");

                            // Перепланировать задачи, которые были пропущены
                            await RescheduleMissedTask(taskScheduler, account, task, taskRepo);
                        }
                    }
                }
            }
            
            Log.Information("Started an infinite loop.");

            DateTime start = DateTime.Now;
            while (true)
            {
                Log.Information($"Infinite loop: I am working. Uptime: {DateTime.Now - start:hh\\:mm\\:ss}");

                Thread.Sleep(60 * 1000); // Wait 1 minute
            }
        }

        private static async Task RescheduleMissedTask(TaskScheduler taskScheduler, Core.Models.Account account, Core.Models.Task task, TaskRepository taskRepo)
        {
            // Если задача пропущена, выполняем её сейчас
            await ExecuteTaskNow(taskScheduler, account, task);

            // Вычисляем следующее время запуска относительно старого времени
            DateTimeOffset nextValidTime = task.NextRunTime;
            while (nextValidTime <= DateTimeOffset.Now)
            {
                nextValidTime = nextValidTime.AddSeconds(task.ScheduleSeconds);
            }

            task.NextRunTime = nextValidTime.DateTime;
            taskRepo.Update(task);

            IJobDetail job = task.TaskType == "DailyCheckJob" ? JobBuilder.Create<DailyCheckJob>().Build() : JobBuilder.Create<FarmingJob>().Build();
            job.JobDataMap.Put("accountId", account.Id);
            job.JobDataMap.Put("taskId" + task.TaskType, task.Id);
            job.JobDataMap.Put("isPlanned", true);

            var trigger = TriggerBuilder.Create()
                .WithSimpleSchedule(schedule => schedule
                    .WithIntervalInSeconds(task.ScheduleSeconds)
                    .RepeatForever())
                .StartAt(nextValidTime.DateTime)
                .Build();

            await taskScheduler.ScheduleTask(task.Id.ToString(), task.Id.ToString(), job, trigger);
        }

        private static async Task ExecuteTaskNow(TaskScheduler taskScheduler, Core.Models.Account account, Core.Models.Task task)
        {
            // Создание задачи для DailyCheckJob
            IJobDetail job = task.TaskType == "DailyCheckJob" ? JobBuilder.Create<DailyCheckJob>().Build() : JobBuilder.Create<FarmingJob>().Build();
            job.JobDataMap.Put("accountId", account.Id);
            job.JobDataMap.Put("taskId" + task.TaskType, task.Id);
            job.JobDataMap.Put("isPlanned", false);

            var trigger = TriggerBuilder.Create()
                                .WithSimpleSchedule(schedule => schedule
                                    .WithIntervalInSeconds(task.ScheduleSeconds)
                                    .WithRepeatCount(0))
                                .StartAt(DateTime.Now.AddSeconds(task.TaskType.Length))
            .Build();

            await taskScheduler.ScheduleTask(account.Id.ToString(), account.Id.ToString(), job, trigger);
        }
    }
}
