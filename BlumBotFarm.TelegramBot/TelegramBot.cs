using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Repositories;
using BlumBotFarm.GameClient;
using BlumBotFarm.Scheduler.Jobs;
using Quartz;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Task = System.Threading.Tasks.Task;
using TaskScheduler = BlumBotFarm.Scheduler.TaskScheduler;

namespace BlumBotFarm.TelegramBot
{
    public class TelegramBot
    {
        private readonly ITelegramBotClient botClient;
        private readonly string[] adminUsernames;
        private readonly AccountRepository accountRepository;
        private readonly TaskRepository    taskRepository;
        private readonly TaskScheduler     taskScheduler;

        public TelegramBot(string token, string[] adminUsernames)
        {
            botClient = new TelegramBotClient(token);
            this.adminUsernames = adminUsernames;
            using (var db = Database.Database.GetConnection())
            {
                accountRepository = new AccountRepository(db);
                taskRepository    = new TaskRepository(db);
            }
            taskScheduler = new TaskScheduler();
        }

        public void Start()
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = [UpdateType.Message]
            };
            botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, cancellationToken: CancellationToken.None);
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is null) return;

            if (update.Type == UpdateType.Message && update.Message.Type == MessageType.Text)
            {
                var message = update.Message;
                if (message.From is null) return;
                var username = message.From.Username;

                if (Array.Exists(adminUsernames, user => user.Equals(username, StringComparison.OrdinalIgnoreCase)))
                {
                    await HandleAdminMessage(message);
                }
            }
        }

        private async Task HandleAdminMessage(Message message)
        {
            if (message.Text is null) return;

            var parts   = message.Text.Split(' ');
            var command = parts[0].ToLower();

            switch (command)
            {
                case "/start":
                    await botClient.SendTextMessageAsync(message.Chat, "Hello. You are admin. You can see the whole list of commands by typing '/'.", null, ParseMode.Html);
                    break;
                case "/addaccount":
                    if (parts.Length == 5 || parts.Length == 6)
                    {
                        var username       = parts[1];
                        var accessToken    = parts[2];
                        var refreshToken   = parts[3];
                        if (!int.TryParse(parts[4], out int timezoneOffset))
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "Something wrong with <b>Timezone Offset</b>.", null, ParseMode.Html);
                            return;
                        }

                        string proxy = "";
                        if (parts.Length == 6) proxy = parts[5];

                        var account = accountRepository.GetAll().FirstOrDefault(user => user.Username == username);
                        if (account != null)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "The account with this username <b>already exists</b>.", null, ParseMode.Html);
                            return;
                        }

                        await AddAccount(username, accessToken, refreshToken, proxy, timezoneOffset);
                        await botClient.SendTextMessageAsync(message.Chat, $"Account <b>{username}</b> added successfully.", null, ParseMode.Html);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Usage: /addaccount <username> <accessToken> <refreshToken> <timezoneOffset> [<proxy>]");
                    }
                    break;
                case "/stats":
                    var stats = GetStatistics();
                    await botClient.SendTextMessageAsync(message.Chat, stats, null, ParseMode.Html);
                    break;
                case "/proxy":
                    if (parts.Length == 2 || parts.Length == 3)
                    {
                        var username = parts[1];
                        var proxy = parts.Length == 3 ? parts[2] : null;

                        var account = accountRepository.GetAll().FirstOrDefault(user => user.Username == username);
                        if (account == null)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "The account with this username <b>does not exist</b>.", null, ParseMode.Html);
                            return;
                        }

                        account.Proxy = proxy ?? string.Empty;
                        accountRepository.Update(account);

                        if (proxy == null)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, $"Proxy for account <b>{username}</b> has been <b>removed</b>.", null, ParseMode.Html);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(message.Chat, $"Proxy for account <b>{username}</b> has been updated to <b>{proxy}</b>.", null, ParseMode.Html);
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Usage: /proxy <username> [<proxy>]");
                    }
                    break;
                default:
                    await botClient.SendTextMessageAsync(message.Chat, "Unknown command.", null, ParseMode.Html);
                    break;
            }
        }

        private async Task AddAccount(string username, string accessToken, string refreshToken, string proxy, int timezoneOffset)
        {
            var account = new Account
            {
                Username       = username,
                AccessToken    = accessToken,
                RefreshToken   = refreshToken,
                UserAgent      = HTTPController.GetRandomUserAgent(),
                Proxy          = proxy,
                TimezoneOffset = timezoneOffset
            };
            accountRepository.Add(account);

            account = accountRepository.GetAll().FirstOrDefault(user => user.Username == username);
            if (account == null) return;

            // Добавление задачи в базу данных
            var now  = DateTime.Now;

            var taskDailyCheckJob = new Core.Models.Task
            {
                AccountId       = account.Id,
                TaskType        = "DailyCheckJob",
                ScheduleSeconds = 24 * 3600, // 24 hours
                NextRunTime     = now.AddDays(1)
            };
            var taskFarming = new Core.Models.Task
            {
                AccountId       = account.Id,
                TaskType        = "Farming",
                ScheduleSeconds = 8 * 3600, // 8 hours
                NextRunTime     = now.AddHours(8)
            };

            // Получаем только что добавленную задачу с присвоенным ID
            taskRepository.Add(taskDailyCheckJob);
            taskDailyCheckJob = taskRepository.GetAll().FirstOrDefault(t => t.AccountId == account.Id && t.TaskType == "DailyCheckJob");
            if (taskDailyCheckJob == null) return;

            taskRepository.Add(taskFarming);
            taskFarming = taskRepository.GetAll().FirstOrDefault(t => t.AccountId == account.Id && t.TaskType == "Farming");
            if (taskFarming == null) return;

            var job1 = JobBuilder.Create<DailyCheckJob>().Build();
            await ScheduleATaskAsync(account, taskDailyCheckJob, job1, now);
            var job2 = JobBuilder.Create<FarmingJob>().Build();
            await ScheduleATaskAsync(account, taskFarming,       job2, now);
        }

        private async Task ScheduleATaskAsync(Account account, Core.Models.Task task, IJobDetail job, DateTime now)
        {
            // Создание задачи для DailyCheckJob
            job.JobDataMap.Put("account", account);
            job.JobDataMap.Put("task" + task.TaskType, task);
            job.JobDataMap.Put("isPlanned", true);

            var trigger = TriggerBuilder.Create()
                                .WithSimpleSchedule(schedule => schedule
                                    .WithIntervalInSeconds(task.ScheduleSeconds)
                                    .RepeatForever())
                                .StartAt(now.AddSeconds(task.TaskType.Length))
                                .Build();

            await taskScheduler.ScheduleTask(account.Id.ToString(), account.Id.ToString(), job, trigger);
        }

        private string GetStatistics()
        {
            var accounts     = accountRepository.GetAll();
            var totalBalance = accounts.Sum(a => a.Balance);
            var totalTickets = accounts.Sum(a => a.Tickets);
            return $"<b>CZ time:</b> <code>{DateTime.UtcNow.AddHours(2):dd.MM.yyyy HH:mm:ss}</code>\n" +
                   $"<b>MSK time:</b> <code>{DateTime.UtcNow.AddHours(3):dd.MM.yyyy HH:mm:ss}</code>\n" +
                   $"Total accounts: <b>{accounts.Count()}</b>\n" +
                   $"Total balance: <b>{totalBalance}</b> $\n" +
                   $"Total tickets: <b>{totalTickets}</b>";
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Log.Error(exception.Message);
            return Task.CompletedTask;
        }
    }
}
