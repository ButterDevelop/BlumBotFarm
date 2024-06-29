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
        private readonly long[]   adminChatIds;
        private readonly AccountRepository accountRepository;
        private readonly TaskRepository    taskRepository;
        private readonly EarningRepository earningRepository;
        private readonly TaskScheduler     taskScheduler;

        public TelegramBot(string token, string[] adminUsernames, long[] adminChatIds)
        {
            botClient = new TelegramBotClient(token);
            this.adminUsernames = adminUsernames;
            this.adminChatIds   = adminChatIds;
            using (var db = Database.Database.GetConnection())
            {
                accountRepository = new AccountRepository(db);
                taskRepository    = new TaskRepository(db);
                earningRepository = new EarningRepository(db);
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

        public async Task SendMessageToAdmins(string message)
        {
            foreach (var adminChatId in adminChatIds)
            {
                await botClient.SendTextMessageAsync(adminChatId, message, null, ParseMode.Html);
            }
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
                else
                {
                    Log.Information("Someone, not admin, tried to execute bot. " +
                                    $"Username: @{username ?? "-"}, userId: {message.From.Id}, " +
                                    $"chatId: {message.Chat.Id}, name: {message.From.FirstName ?? "-"} {message.From.LastName ?? "-"}\n" +
                                    $"Their message to bot: {message.Text}");

                    await SendMessageToAdmins("<b>Someone, not admin, tried to execute bot.</b>\n" +
                                              $"Their info - username: <b>@{username ?? "-"}</b>, userId: <b>{message.From.Id}</b>, " +
                                              $"chatId: <b>{message.Chat.Id}</b>, " +
                                              $"name: <b>{message.From.FirstName ?? "-"} {message.From.LastName ?? "-"}</b>\n" +
                                              $"Their message to bot: <code>{message.Text}</code>");
                }
            }
        }

        private async Task HandleAdminMessage(Telegram.Bot.Types.Message message)
        {
            if (message.Text is null || message.From is null) return;

            var parts   = message.Text.Split(' ');
            var command = parts[0].ToLower();

            Log.Information($"Command called by {message.From.Username}: {message.Text}");

            switch (command)
            {
                case "/start":
                    await botClient.SendTextMessageAsync(message.Chat, "Hello. You are admin. You can see the whole list of commands by typing '/'.\n" +
                                                                       $"Your Telegram Chat Id with me is: <code>{message.Chat.Id}</code>",
                                                                       null, ParseMode.Html);
                    break;
                case "/addaccount":
                    if (parts.Length == 3 || parts.Length == 4)
                    {
                        var username     = parts[1];
                        var refreshToken = parts[2];
                        var proxy        = parts.Length == 4 ? parts[3] : "";

                        if (username is null || refreshToken is null || proxy is null)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "Something went wrong with your data, please, try again.", null, ParseMode.Html);
                            return;
                        }

                        var account = accountRepository.GetAll().FirstOrDefault(user => user.Username == username);
                        if (account != null)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "The account with this username <b>already exists</b>.", null, ParseMode.Html);
                            return;
                        }

                        await AddAccount(username, refreshToken, proxy);
                        await botClient.SendTextMessageAsync(message.Chat, $"Account <b>{username}</b> added successfully.", null, ParseMode.Html);

                        Log.Information($"Account {username} added successfully.");
                    }
                    else
                    if (parts.Length == 5 || parts.Length == 6)
                    {
                        var username     = parts[1];
                        var accessToken  = parts[2];
                        var refreshToken = parts[3];
                        var proxy        = parts.Length == 6 ? parts[5] : "";
                        if (!int.TryParse(parts[4], out int timezoneOffset))
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "Something wrong with <b>Timezone Offset</b>.", null, ParseMode.Html);
                            return;
                        }

                        if (username is null || accessToken is null || refreshToken is null || proxy is null)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "Something went wrong with your data, please, try again.", null, ParseMode.Html);
                            return;
                        }

                        var account = accountRepository.GetAll().FirstOrDefault(user => user.Username == username);
                        if (account != null)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "The account with this username <b>already exists</b>.", null, ParseMode.Html);
                            return;
                        }

                        await AddAccount(username, accessToken, refreshToken, proxy, timezoneOffset);
                        await botClient.SendTextMessageAsync(message.Chat, $"Account <b>{username}</b> added successfully.", null, ParseMode.Html);

                        Log.Information($"Account {username} added successfully.");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Usage: /addaccount <username> <accessToken> <refreshToken> <timezoneOffset> [<proxy>]\n" +
                                                                           "Or: /addaccount <username> <refreshToken> [<proxy>]");
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
                            Log.Information($"Proxy for account {username} has been removed.");
                            await botClient.SendTextMessageAsync(message.Chat, $"Proxy for account <b>{username}</b> has been <b>removed</b>.", null, ParseMode.Html);
                        }
                        else
                        {
                            Log.Information($"Proxy for account {username} has been updated to {proxy}.");
                            await botClient.SendTextMessageAsync(message.Chat, $"Proxy for account <b>{username}</b> has been updated to <b>{proxy}</b>.", null, ParseMode.Html);
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Usage: /proxy <username> [<proxy>]");
                    }
                    break;
                case "/info":
                    if (parts.Length == 2)
                    {
                        var username = parts[1];

                        if (string.IsNullOrEmpty(username))
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "The username <b>is empty</b>.", null, ParseMode.Html);
                            return;
                        }

                        var account = accountRepository.GetAll().FirstOrDefault(user => user.Username == username);
                        if (account == null)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "The account with this username <b>does not exist</b>.", null, ParseMode.Html);
                            return;
                        }

                        await botClient.SendTextMessageAsync(message.Chat, $"You called info.\n" +
                                                                           $"Id: <b>{account.Id}</b>\n" + 
                                                                           $"Username: <b>{account.Username}</b>\n" + 
                                                                           $"Balance: <b>{account.Balance}</b> ฿\n" + 
                                                                           $"Tickets: <b>{account.Tickets}</b>\n" +
                                                                           $"UserAgent: <code>{account.UserAgent}</code>\n" + 
                                                                           $"Proxy: <code>{account.Proxy}</code>\n" +
                                                                           $"Timezone offset: <b>{account.TimezoneOffset}</b>",
                                                                           null, ParseMode.Html);

                        Log.Information($"{message.From.Username} called info for {username}.");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Usage: /info <username>");
                    }
                    break;
                case "/ticketsinfo":

                    break;
                case "/accountsinfo":

                    break;
                case "/forcedailyjob":
                    if (parts.Length == 2)
                    {
                        var username = parts[1];

                        if (string.IsNullOrEmpty(username))
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "The username <b>is empty</b>.", null, ParseMode.Html);
                            return;
                        }

                        Log.Information($"{message.From.Username} forced unscheduled Daily Job for {username}.");
                        await SendMessageToAdmins($"<b>{message.From.Username}</b> forced unscheduled Daily Job for <b>{username}</b>.");

                        var account = accountRepository.GetAll().FirstOrDefault(user => user.Username == username);
                        if (account == null)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "The account with this username <b>does not exist</b>.", null, ParseMode.Html);
                            return;
                        }

                        var job = JobBuilder.Create<DailyCheckJob>().Build();
                        
                        var task = new Core.Models.Task
                        {
                            AccountId       = account.Id,
                            ScheduleSeconds = -1,
                            NextRunTime     = new DateTime(1990, 1, 1),
                            TaskType        = "DailyCheckJob"
                        };
                        
                        // Создание задачи для DailyCheckJob
                        job.JobDataMap.Put("accountId", account.Id);
                        job.JobDataMap.Put("taskId" + task.TaskType, task.Id);
                        job.JobDataMap.Put("isPlanned", false);
                        
                        var now = DateTime.Now;
                        
                        var trigger = TriggerBuilder.Create()
                                .WithSimpleSchedule(schedule => schedule.WithRepeatCount(0))
                                .StartAt(now.AddSeconds(task.TaskType.Length))
                                .Build();
                        
                        await taskScheduler.ScheduleTask(account.Id.ToString(), account.Id.ToString(), job, trigger);

                        await SendMessageToAdmins($"Forced unscheduled Daily Job for <b>{username}</b> will start soon.");
                        Log.Information($"Forced unscheduled Daily Job for {username} will start soon.");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Usage: /forcedailyjob <username>");
                    }
                    break;
                case "/forcedailyjobforeveryone":
                    Log.Information($"{message.From.Username} forced unscheduled Daily Job.");
                    await SendMessageToAdmins($"<b>{message.From.Username}</b> forced unscheduled Daily Job.");

                    var accounts = accountRepository.GetAll().OrderBy(account => account.Id);
                    int counter = 0;
                    foreach (var account in accounts)
                    {
                        ++counter;

                        var job = JobBuilder.Create<DailyCheckJob>().Build();

                        var task = new Core.Models.Task
                        {
                            AccountId       = account.Id,
                            ScheduleSeconds = -1,
                            NextRunTime     = new DateTime(1990, 1, 1),
                            TaskType        = "DailyCheckJob"
                        };

                        // Создание задачи для DailyCheckJob
                        job.JobDataMap.Put("accountId", account.Id);
                        job.JobDataMap.Put("taskId" + task.TaskType, task.Id);
                        job.JobDataMap.Put("isPlanned", false);

                        var now = DateTime.Now;

                        var trigger = TriggerBuilder.Create()
                            .WithSimpleSchedule(schedule => schedule.WithRepeatCount(0))
                            .StartAt(now.AddSeconds(task.TaskType.Length + (counter * 10)))
                            .Build();

                        await taskScheduler.ScheduleTask(account.Id.ToString(), account.Id.ToString(), job, trigger);
                    }

                    await SendMessageToAdmins($"Forced unscheduled Daily Job will start soon.");
                    Log.Information($"Forced unscheduled Daily Job will start soon.");
                    break;
                case "/refreshtoken":
                    if (parts.Length == 3)
                    {
                        var username     = parts[1];
                        var refreshToken = parts[2];

                        if (username is null || refreshToken is null)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "Something went wrong with your data, please, try again.", null, ParseMode.Html);
                            return;
                        }

                        var account = accountRepository.GetAll().FirstOrDefault(user => user.Username == username);
                        if (account == null)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "The account with this username <b>does not exist</b>.", null, ParseMode.Html);
                            return;
                        }

                        account.RefreshToken = refreshToken;
                        accountRepository.Update(account);

                        await botClient.SendTextMessageAsync(message.Chat, $"<b>{username}</b>'s refresh token updated successfully.", null, ParseMode.Html);

                        Log.Information($"{username}'s refresh token updated successfully.");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Usage: /refreshtoken <username> <refreshToken>");
                    }
                    break;
                case "/restartapp":
                    await SendMessageToAdmins($"<b>{message.From.Username}</b> forced restarting the app.");
                    Log.Information($"{message.From.Username} forced restarting the app.");
                    new Thread(() =>
                    {
                        Thread.Sleep(10000);
                        Environment.Exit(0);
                    }).Start();
                    break;
                default:
                    await botClient.SendTextMessageAsync(message.Chat, "Unknown command.", null, ParseMode.Html);
                    break;
            }
        }

        private async Task AddAccount(string username, string refreshToken, string proxy)
        {
            await AddAccount(username, accessToken: "", refreshToken, proxy, timezoneOffset: -120);
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
            if (taskDailyCheckJob == null)
            {
                Log.Error("TelegramBot AddAccount: task Daily Check Job is NULL after getting it from the DB!");
                return;
            }

            taskRepository.Add(taskFarming);
            taskFarming = taskRepository.GetAll().FirstOrDefault(t => t.AccountId == account.Id && t.TaskType == "Farming");
            if (taskFarming == null)
            {
                Log.Error("TelegramBot AddAccount: task Farming is NULL after getting it from the DB!");
                return;
            }

            var job1 = JobBuilder.Create<DailyCheckJob>().Build();
            await ScheduleATaskAsync(account, taskDailyCheckJob, job1, now);
            var job2 = JobBuilder.Create<FarmingJob>().Build();
            await ScheduleATaskAsync(account, taskFarming,       job2, now.AddMilliseconds(TaskScheduler.MAX_MS_AMOUNT_TO_WAIT_BEFORE_JOB));

            Log.Information($"AddAccount: Scheduled tasks and added it to the DB: {username}");
        }

        private async Task ScheduleATaskAsync(Account account, Core.Models.Task task, IJobDetail job, DateTime now)
        {
            // Создание задачи для DailyCheckJob
            job.JobDataMap.Put("accountId", account.Id);
            job.JobDataMap.Put("taskId" + task.TaskType, task.Id);
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

            var now        = DateTime.Now;
            var startOfDay = now.Date;
            var endOfDay   = startOfDay.AddDays(1);

            var tasks = taskRepository.GetAll();

            int executedDailyJobs    = 0;
            int notExecutedDailyJobs = 0;
            int executedFarming      = 0;
            int notExecutedFarming   = 0;

            foreach (var task in tasks)
            {
                if (task.TaskType == "DailyCheckJob" || task.TaskType == "Farming")
                {
                    // Посчет выполненных задач от текущего времени до начала дня
                    var previousRunTime = task.NextRunTime;
                    while (previousRunTime > startOfDay)
                    {
                        if (previousRunTime <= now)
                        {
                            if (task.TaskType == "DailyCheckJob")
                                executedDailyJobs++;
                            else if (task.TaskType == "Farming")
                                executedFarming++;
                        }

                        previousRunTime = previousRunTime.AddSeconds(-task.ScheduleSeconds);
                    }

                    // Подсчет оставшихся задач от текущего времени до конца дня
                    var nextRunTime = task.NextRunTime;
                    while (nextRunTime < endOfDay)
                    {
                        if (nextRunTime >= now)
                        {
                            if (task.TaskType == "DailyCheckJob")
                                notExecutedDailyJobs++;
                            else if (task.TaskType == "Farming")
                                notExecutedFarming++;
                        }

                        nextRunTime = nextRunTime.AddSeconds(task.ScheduleSeconds);
                    }
                }
            }

            var todayDateTime    = DateTime.Today;
            var todayEarnings    = earningRepository.GetAll().Where(earning => earning.Created > todayDateTime);
            var todayEarningsSum = todayEarnings.Sum(earning => earning.Total);

            return $"<b>CZ time:</b> <code>{DateTime.UtcNow.AddHours(2):dd.MM.yyyy HH:mm:ss}</code>\n" +
                   $"<b>MSK time:</b> <code>{DateTime.UtcNow.AddHours(3):dd.MM.yyyy HH:mm:ss}</code>\n" +
                   $"Total accounts: <b>{accounts.Count()}</b>\n" +
                   $"Total balance: <b>{totalBalance:N2}</b> ฿\n" +
                   $"Total tickets: <b>{totalTickets}</b>\n" +
                   $"Executed jobs today: <b>{executedDailyJobs + executedFarming}</b>\n" +
                   $"Remaining jobs today: <b>{notExecutedDailyJobs + notExecutedFarming}</b>\n" + 
                   $"Today earnings: ≈<b>{todayEarningsSum:N2}</b> ฿";
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Log.Error($"Telegram Bot HandleErrorAsync: {exception}");
            return Task.CompletedTask;
        }
    }
}
