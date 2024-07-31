using BlumBotFarm.Core;
using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Repositories;
using BlumBotFarm.GameClient;
using Serilog;
using System.Security.Cryptography;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Task = System.Threading.Tasks.Task;
using TaskScheduler = BlumBotFarm.Scheduler.TaskScheduler;

namespace BlumBotFarm.TelegramBot
{
    public class AdminTelegramBot
    {
        private readonly ITelegramBotClient botClient;
        private readonly long[]   adminChatIds;
        private readonly AccountRepository     accountRepository;
        private readonly TaskRepository        taskRepository;
        private readonly EarningRepository     earningRepository;
        private readonly DailyRewardRepository dailyRewardRepository;
        private readonly UserRepository        userRepository;
        private readonly TaskScheduler         taskScheduler;

        public AdminTelegramBot(TelegramBotClient botClient, long[] adminChatIds)
        {
            this.botClient        = botClient;
            this.adminChatIds     = adminChatIds;
            var db                = Database.Database.ConnectionString;
            accountRepository     = new AccountRepository(db);
            taskRepository        = new TaskRepository(db);
            earningRepository     = new EarningRepository(db);
            dailyRewardRepository = new DailyRewardRepository(db);
            userRepository        = new UserRepository(db);
            taskScheduler         = new TaskScheduler();
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

                if (Array.Exists(adminChatIds, userId => userId == message.From.Id))
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
                    if (parts.Length == 4 || parts.Length == 5)
                    {
                        var username     = parts[1];
                        var refreshToken = parts[2];
                        var proxy        = parts.Length == 5 ? parts[4] : "";

                        if (!int.TryParse(parts[3], out int userId))
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "Something wrong with <b>UserId</b>.", null, ParseMode.Html);
                            return;
                        }
                        if (userRepository.GetById(userId) == null)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "No such <b>UserId</b> in the database!", null, ParseMode.Html);
                            return;
                        }

                        if (username is null || refreshToken is null || proxy is null)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "Something went wrong with your data, please, try again.", null, ParseMode.Html);
                            return;
                        }

                        var account = accountRepository.GetAll().FirstOrDefault(user => user.CustomUsername == username);
                        if (account != null)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "The account with this username <b>already exists</b>.", null, ParseMode.Html);
                            return;
                        }

                        await AddAccount(username, refreshToken, proxy, userId);
                        await botClient.SendTextMessageAsync(message.Chat, $"Account <b>{username}</b> added successfully.", null, ParseMode.Html);

                        Log.Information($"Account {username} added successfully.");
                    }
                    else
                    if (parts.Length == 6 || parts.Length == 7)
                    {
                        var username     = parts[1];
                        var accessToken  = parts[2];
                        var refreshToken = parts[3];
                        var proxy        = parts.Length == 7 ? parts[6] : "";

                        if (!int.TryParse(parts[4], out int timezoneOffset))
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "Something wrong with <b>Timezone Offset</b>.", null, ParseMode.Html);
                            return;
                        }

                        if (!int.TryParse(parts[5], out int userId))
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "Something wrong with <b>UserId</b>.", null, ParseMode.Html);
                            return;
                        }
                        if (userRepository.GetById(userId) == null)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "No such <b>UserId</b> in the database!", null, ParseMode.Html);
                            return;
                        }

                        if (username is null || accessToken is null || refreshToken is null || proxy is null)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "Something went wrong with your data, please, try again.", null, ParseMode.Html);
                            return;
                        }


                        var account = accountRepository.GetAll().FirstOrDefault(user => user.CustomUsername == username);
                        if (account != null)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "The account with this username <b>already exists</b>.", null, ParseMode.Html);
                            return;
                        }

                        await AddAccount(username, accessToken, refreshToken, proxy, timezoneOffset, userId);
                        await botClient.SendTextMessageAsync(message.Chat, $"Account <b>{username}</b> added successfully.", null, ParseMode.Html);

                        Log.Information($"Account {username} added successfully.");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Usage: /addaccount <username> <accessToken> <refreshToken> <timezoneOffset> <userId> [<proxy>]\n" +
                                                                           "Or: /addaccount <username> <refreshToken> <userId> [<proxy>]");
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

                        var accountsProxy = accountRepository.GetAll();
                        var account       = accountsProxy.FirstOrDefault(user => user.CustomUsername == username) ?? 
                                            accountsProxy.FirstOrDefault(user => user.BlumUsername   == username);
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
                case "/todayresults":
                    var accountsTodayResults = accountRepository.GetAll();
                    var tasksTodayResults    = taskRepository.GetAll();

                    var today = DateTime.Today;
                    var dailyRewardsToday = dailyRewardRepository.GetAll().Where(dr => dr.CreatedAt >= today).DistinctBy(dr => dr.AccountId);

                    List<(long dailyInTicks, string line)> todayResultsLines = [];
                    foreach (var account in accountsTodayResults)
                    {
                        if (!dailyRewardsToday.Any(dr => dr.AccountId == account.Id))
                        {
                            var dailyCheckJob = tasksTodayResults.FirstOrDefault(task => task.AccountId == account.Id && task.TaskType == "DailyCheckJob");
                            if (dailyCheckJob == null) continue;

                            var    dailyIn    = dailyCheckJob.NextRunTime - DateTime.Now;
                            string dailyMinus = dailyIn < TimeSpan.Zero ? "-" : "";

                            todayResultsLines.Add((dailyIn.Ticks, $"Custom Username: <code>{account.CustomUsername}</code>, " +
                                                                  $"Blum Username: <code>{account.BlumUsername}</code>, " + 
                                                                  $"Job in: <b>{dailyMinus}{dailyIn:hh\\:mm\\:ss}</b>"));
                        }
                    }

                    int doneCount = dailyRewardsToday.Count(), wholeCount = accountsTodayResults.Count(acc => !string.IsNullOrEmpty(acc.ProviderToken));
                    StringBuilder messageToSendTodayResults = new((doneCount >= wholeCount ? "<b>Work for today is done.</b>\n" : "") + 
                                                                  "Taken daily rewards today: " +
                                                                  $"<b>{doneCount}/{wholeCount}</b>\n" +
                                                                  "Those who didn't take daily reward:" + 
                                                                  (todayResultsLines.Count == 0 ? " <b>no</b>" : "\n"));
                    foreach (var line in todayResultsLines.OrderBy(info => info.dailyInTicks).Select(info => info.line))
                    {
                        messageToSendTodayResults.AppendLine(line);

                        if (messageToSendTodayResults.Length >= 2048) // TG message length limit is 4096
                        {
                            await botClient.SendTextMessageAsync(message.Chat, messageToSendTodayResults.ToString(), null, ParseMode.Html);

                            messageToSendTodayResults.Clear();
                        }
                    }

                    await botClient.SendTextMessageAsync(message.Chat, messageToSendTodayResults.ToString(), null, ParseMode.Html);

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

                        var accountsInfo = accountRepository.GetAll();
                        var account      = accountsInfo.FirstOrDefault(user => user.CustomUsername == username) ??
                                           accountsInfo.FirstOrDefault(user => user.BlumUsername   == username);
                        if (account == null)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "The account with this username <b>does not exist</b>.", null, ParseMode.Html);
                            return;
                        }

                        await botClient.SendTextMessageAsync(message.Chat, $"You called info.\n" +
                                                                           $"Id: <b>{account.Id}</b>\n" + 
                                                                           $"Custom username: <code>{account.CustomUsername}</code>\n" +
                                                                           $"Blum username: <code>{account.BlumUsername}</code>\n" +
                                                                           $"Balance: <b>{account.Balance}</b> ฿\n" + 
                                                                           $"Tickets: <b>{account.Tickets}</b>\n" +
                                                                           $"Referrals count: <b>{account.ReferralsCount}</b>\n" +
                                                                           $"Referral link: <code>{account.ReferralLink}</code>\n" +
                                                                           $"UserAgent: <code>{account.UserAgent}</code>\n" + 
                                                                           $"Proxy: <code>{account.Proxy}</code>\n" +
                                                                           $"Timezone offset: <b>{account.TimezoneOffset}</b>\n" +
                                                                           $"Last status: <b>{account.LastStatus}</b>",
                                                                           null, ParseMode.Html);

                        Log.Information($"{message.From.Username} called info for {username}.");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Usage: /info <username>");
                    }
                    break;
                case "/unspenttickets":
                    var accountsWithTicketsNotZero = accountRepository.GetAll().Where(acc => acc.Tickets > 0);
                    var totalTickets = accountsWithTicketsNotZero.Select(acc => acc.Tickets).DefaultIfEmpty(0).Sum();

                    var tasks = taskRepository.GetAll();

                    List<(long dailyInTicks, string line)> unspentTicketsLines = [];
                    if (totalTickets > 0)
                    {
                        foreach (var account in accountsWithTicketsNotZero)
                        {
                            var dailyCheckJob = tasks.FirstOrDefault(task => task.AccountId == account.Id && task.TaskType == "DailyCheckJob");
                            if (dailyCheckJob == null) continue;

                            var    dailyIn    = dailyCheckJob.NextRunTime - DateTime.Now;
                            string dailyMinus = dailyIn   < TimeSpan.Zero ? "-" : "";

                            unspentTicketsLines.Add((dailyIn.Ticks, $"(<code>{account.CustomUsername}</code>, " +
                                                                    $"Blum <code>{account.BlumUsername}</code>), " +
                                                                    $"tickets: <b>{account.Tickets}</b>, " +
                                                                    $"Job in: <b>{dailyMinus}{dailyIn:hh\\:mm\\:ss}</b>"));
                        }
                    }

                    StringBuilder messageToSendTickets = new($"Unspent tickets in total: <b>{totalTickets}</b>\n");
                    foreach (var (dailyInTicks, line) in unspentTicketsLines.OrderBy(info => info.dailyInTicks))
                    {
                        messageToSendTickets.AppendLine(line);

                        if (messageToSendTickets.Length >= 2048) // TG message length limit is 4096
                        {
                            await botClient.SendTextMessageAsync(message.Chat, messageToSendTickets.ToString(), null, ParseMode.Html);

                            messageToSendTickets.Clear();
                        }
                    }

                    await botClient.SendTextMessageAsync(message.Chat, messageToSendTickets.ToString(), null, ParseMode.Html);

                    break;
                case "/jobsinfo":
                    var accountsJobsInfo = accountRepository.GetAll();
                    var tasksJobsInfo    = taskRepository.GetAll();

                    List<(long dailyInTicks, string line)> jobsInfoLines = [];
                    foreach (var account in accountsJobsInfo)
                    {
                        var dailyCheckJob = tasksJobsInfo.FirstOrDefault(task => task.AccountId == account.Id && task.TaskType == "DailyCheckJob");
                        if (dailyCheckJob == null) continue;

                        var    dailyIn    = dailyCheckJob.NextRunTime - DateTime.Now;
                        string dailyMinus = dailyIn < TimeSpan.Zero ? "-" : "";

                        jobsInfoLines.Add((dailyIn.Ticks,
                                          $"(<code>{account.CustomUsername}</code>, " +
                                          $"Blum <code>{account.BlumUsername}</code>), " +
                                          $"tickets: <b>{account.Tickets}</b>, " +
                                          $"Job in: <b>{dailyMinus}{dailyIn:hh\\:mm\\:ss}</b>"));
                    }

                    StringBuilder messageToSendJobsInfo = new("<b>Jobs info:</b>\n");
                    foreach (var (dailyInTicks, line) in jobsInfoLines.OrderBy(info => info.dailyInTicks))
                    {
                        messageToSendJobsInfo.AppendLine(line);

                        if (messageToSendJobsInfo.Length >= 2048) // TG message length limit is 4096
                        {
                            await botClient.SendTextMessageAsync(message.Chat, messageToSendJobsInfo.ToString(), null, ParseMode.Html);

                            messageToSendJobsInfo.Clear();
                        }
                    }

                    await botClient.SendTextMessageAsync(message.Chat, messageToSendJobsInfo.ToString(), null, ParseMode.Html);

                    break;
                case "/accountsinfo":
                    var accountsList = accountRepository.GetAll().OrderBy(acc => acc.Id);

                    StringBuilder messageToSendAccounts = new("Accounts full info:\n");
                    foreach (var account in accountsList)
                    {
                        messageToSendAccounts.AppendLine($"Id: <b>{account.Id}</b>, " +
                                                         $"(<code>{account.CustomUsername}</code>, " +
                                                         $"Blum <code>{account.BlumUsername}</code>), " +
                                                         $"<b>{account.Balance}</b> ฿, tickets: <b>{account.Tickets}</b>");

                        if (messageToSendAccounts.Length >= 2048) // TG message length limit is 4096
                        {
                            await botClient.SendTextMessageAsync(message.Chat, messageToSendAccounts.ToString(), null, ParseMode.Html);

                            messageToSendAccounts.Clear();
                        }
                    }

                    await botClient.SendTextMessageAsync(message.Chat, messageToSendAccounts.ToString(), null, ParseMode.Html);

                    break;
                case "/usersinfo":
                    var usersInfo         = userRepository.GetAll().OrderBy(acc => acc.Id);
                    var accountsUsersInfo = accountRepository.GetAll();

                    await botClient.SendTextMessageAsync(message.Chat, $"Whole users: <b>{usersInfo.Count()}</b>\n" +
                                                                       $"Whole accounts: <b>{accountsUsersInfo.Count()}</b>", null, ParseMode.Html);

                    break;
                case "/updateusersinfo":
                    await TaskScheduler.UpdateUsersInfoNow();
                    Log.Information($"{message.From.Username} forced update users info.");
                    await SendMessageToAdmins($"<b>{message.From.Username}</b> forced updating users info.");
                    break;
                case "/authcheck":
                    if (parts.Length == 2)
                    {
                        var username = parts[1];

                        if (string.IsNullOrEmpty(username))
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "The username <b>is empty</b>.", null, ParseMode.Html);
                            return;
                        }

                        var accountsAuthCheck = accountRepository.GetAll();
                        var account           = accountsAuthCheck.FirstOrDefault(user => user.CustomUsername == username) ??
                                                accountsAuthCheck.FirstOrDefault(user => user.BlumUsername   == username);
                        if (account == null)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "The account with this username <b>does not exist</b>.", null, ParseMode.Html);
                            return;
                        }

                        Log.Information($"{message.From.Username} forced an auth check for {username}.");

                        var result = GameApiUtilsService.AuthCheck(account, accountRepository, new GameApiClient());
                        if (result == ApiResponse.Success)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, $"Auth check for <b>{username}</b> succeeded.", null, ParseMode.Html);
                            Log.Error($"TelegramBot: Auth check for {username} succeeded.");
                        }
                        else
                        if (result == ApiResponse.Error)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, $"Auth check for <b>{username}</b> RETURNED FAILURE.\n" +
                                                                               "Probably because of proxy.", null, ParseMode.Html);
                            Log.Error($"TelegramBot: Auth check for {username} RETURNED FAILURE. Probably because of proxy.");
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(message.Chat, $"Auth check for <b>{username}</b> RETURNED FAILURE.", null, ParseMode.Html);
                            Log.Error($"TelegramBot: Auth check for {username} RETURNED FAILURE.");
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Usage: /authcheck <username>");
                    }
                    break;
                case "/forcedailyjobtoguyswithtickets":
                    Log.Information($"{message.From.Username} forced Daily Check Job for accounts which has more than 0 tickets.");
                    await SendMessageToAdmins($"<b>{message.From.Username}</b> forced Daily Check Job for accounts which has more than 0 tickets.");

                    var accountsWithTickets = accountRepository.GetAll().Where(acc => acc.Tickets > 0);
                    int counterWithTickets  = 0;
                    foreach (var account in accountsWithTickets)
                    {
                        ++counterWithTickets;

                        await ScheduleDailyTaskForAnAccount(account, DateTime.Now.AddSeconds(counterWithTickets * 10));
                    }

                    await SendMessageToAdmins($"Forced Daily Check Job for accounts which has more than 0 tickets will start soon.");
                    Log.Information($"Forced Daily Check Job for accounts which has more than 0 tickets will start soon.");

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

                        var accountsForceDailyJob = accountRepository.GetAll();
                        var account               = accountsForceDailyJob.FirstOrDefault(user => user.CustomUsername == username) ??
                                                    accountsForceDailyJob.FirstOrDefault(user => user.BlumUsername   == username);
                        if (account == null)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "The account with this username <b>does not exist</b>.", null, ParseMode.Html);
                            return;
                        }

                        await ScheduleDailyTaskForAnAccountRightNow(account);

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
                        
                        await ScheduleDailyTaskForAnAccount(account, DateTime.Now.AddSeconds(counter * 10));
                    }

                    await SendMessageToAdmins($"Forced unscheduled Daily Job will start soon.");
                    Log.Information($"Forced unscheduled Daily Job will start soon.");
                    break;
                case "/redistributetasks":
                    Log.Information($"{message.From.Username} forced redistributing tasks.");

                    bool deleteAllTasksResult = await taskScheduler.DeleteAllTasks();
                    if (deleteAllTasksResult)
                    {
                        await TaskScheduler.ExecuteMainJobNow();

                        await SendMessageToAdmins($"<b>{message.From.Username}</b> forced redistributing tasks.");
                        Log.Information("Delete all tasks returned success! Executed.");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Delete all tasks returned failure. Cancelling.", null, ParseMode.Html);
                        Log.Information("Delete all tasks returned failure. Cancelling.");
                    }
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

                        var accountsRefreshToken = accountRepository.GetAll();
                        var account              = accountsRefreshToken.FirstOrDefault(user => user.CustomUsername == username) ??
                                                   accountsRefreshToken.FirstOrDefault(user => user.BlumUsername   == username);
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
                case "/providertoken":
                    if (parts.Length == 3)
                    {
                        var username      = parts[1];
                        var providerToken = parts[2];

                        if (username is null || providerToken is null)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "Something went wrong with your data, please, try again.", null, ParseMode.Html);
                            return;
                        }

                        var accountsProviderToken = accountRepository.GetAll();
                        var account               = accountsProviderToken.FirstOrDefault(user => user.CustomUsername == username) ??
                                                    accountsProviderToken.FirstOrDefault(user => user.BlumUsername   == username);
                        if (account == null)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "The account with this username <b>does not exist</b>.", null, ParseMode.Html);
                            return;
                        }

                        account.ProviderToken = providerToken;
                        accountRepository.Update(account);

                        await botClient.SendTextMessageAsync(message.Chat, $"<b>{username}</b>'s provider token updated successfully.", null, ParseMode.Html);

                        Log.Information($"{username}'s provider token updated successfully.");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Usage: /providertoken <username> <providertoken>");
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
                case "/userusdbalance":
                    if (parts.Length == 2 || parts.Length == 4)
                    {
                        var TGidString = parts[1];

                        if (!long.TryParse(TGidString, out long tgUserId))
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "Something went wrong with User ID, please, try again.", null, ParseMode.Html);
                            return;
                        }

                        var user = userRepository.GetAll().FirstOrDefault(u => u.TelegramUserId == tgUserId);
                        if (user == null)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "The user with this TG ID <b>does not exist</b>.", null, ParseMode.Html);
                            return;
                        }

                        if (parts.Length == 4)
                        {
                            string newBalanceString       = parts[2];
                            string sha256hashOfNewBalance = parts[3];

                            if (!decimal.TryParse(newBalanceString, out decimal newBalance))
                            {
                                await botClient.SendTextMessageAsync(message.Chat, "Can't parse new balance.", null, ParseMode.Html);
                                return;
                            }
                            if (sha256hashOfNewBalance is null)
                            {
                                await botClient.SendTextMessageAsync(message.Chat, "Something is wrong with your hash.", null, ParseMode.Html);
                                return;
                            }

                            var ourHashBytes  = SHA256.HashData(Encoding.UTF8.GetBytes(newBalanceString));
                            var userHashBytes = Convert.FromHexString(sha256hashOfNewBalance.ToLower());
                            if (!ourHashBytes.SequenceEqual(userHashBytes))
                            {
                                await botClient.SendTextMessageAsync(message.Chat, "Wrong hash.", null, ParseMode.Html);
                                return;
                            }

                            user.BalanceUSD = newBalance;
                            userRepository.Update(user);

                            await SendMessageToAdmins($"<b>{tgUserId}</b>'s USD balance updated successfully.\n" +
                                                      $"Update called by <b>{message.From.Username}</b>.");

                            Log.Information($"{tgUserId}'s USD balance updated successfully. Update called by {message.From.Username}");
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(message.Chat, $"<b>{tgUserId}</b>'s USD balance is <b>{user.BalanceUSD}</b>.", 
                                                                               null, ParseMode.Html);
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Usage: /userusdbalance <tgUserId>\n" +
                                                                           "Or: /userusdbalance <tgUserId> <newBalance> <sha256hashOfNewBalance>");
                    }
                    break;
                default:
                    await botClient.SendTextMessageAsync(message.Chat, "Unknown command.", null, ParseMode.Html);
                    break;
            }
        }

        private async Task ScheduleDailyTaskForAnAccount(Account account, DateTime startAt)
        {
            var task = new Core.Models.Task
            {
                AccountId          = account.Id,
                MinScheduleSeconds = -1,
                MaxScheduleSeconds = -1,
                NextRunTime        = new DateTime(1990, 1, 1),
                TaskType           = "DailyCheckJob"
            };

            await TaskScheduler.ScheduleNewTask(taskScheduler, account.Id, task, startAt, rightNow: false, isPlanned: false);
        }

        private async Task ScheduleDailyTaskForAnAccountRightNow(Account account)
        {
            var task = new Core.Models.Task
            {
                AccountId          = account.Id,
                MinScheduleSeconds = -1,
                MaxScheduleSeconds = -1,
                NextRunTime        = new DateTime(1990, 1, 1),
                TaskType           = "DailyCheckJob"
            };

            var now = DateTime.Now;
            await TaskScheduler.ScheduleNewTask(taskScheduler, account.Id, task, now, rightNow: true, isPlanned: false);
        }

        private async Task AddAccount(string username, string refreshToken, string proxy, int userId)
        {
            await AddAccount(username, accessToken: "", refreshToken, proxy, timezoneOffset: -120, userId);
        }
        private async Task AddAccount(string username, string accessToken, string refreshToken, string proxy, int timezoneOffset, int userId)
        {
            var account = new Account
            {
                UserId         = userId,
                CustomUsername = username,
                BlumUsername   = string.Empty,
                Balance        = 0,
                Tickets        = 0,
                ReferralsCount = 0,
                ReferralLink   = string.Empty,
                AccessToken    = accessToken,
                RefreshToken   = refreshToken,
                ProviderToken  = string.Empty,
                UserAgent      = HTTPController.GetRandomUserAgent(),
                Proxy          = proxy,
                TimezoneOffset = timezoneOffset
            };
            int accountId = accountRepository.Add(account);
            account       = accountRepository.GetById(accountId);
            if (account == null) return;

            // Добавление задачи в базу данных
            var now = DateTime.Now;

            var taskDailyCheckJob = new Core.Models.Task
            {
                AccountId          = account.Id,
                TaskType           = "DailyCheckJob",
                MinScheduleSeconds = 6  * 3600, // 6 hours
                MaxScheduleSeconds = 10 * 3600, // 10 hours
                NextRunTime        = now.AddDays(1)
            };

            // Получаем только что добавленную задачу с присвоенным ID
            taskRepository.Add(taskDailyCheckJob);
            taskDailyCheckJob = taskRepository.GetAll().FirstOrDefault(t => t.AccountId == account.Id && t.TaskType == "DailyCheckJob");
            if (taskDailyCheckJob == null)
            {
                Log.Error("TelegramBot AddAccount: task Daily Check Job is NULL after getting it from the DB!");
                return;
            }

            await TaskScheduler.ScheduleNewTask(taskScheduler, account.Id, taskDailyCheckJob, now.AddSeconds(taskDailyCheckJob.TaskType.Length), 
                                                rightNow: true);

            Log.Information($"AddAccount: Scheduled tasks and added it to the DB: {username}");
        }

        private string GetStatistics()
        {
            var accounts     = accountRepository.GetAll();
            var totalBalance = accounts.Select(a => a.Balance).DefaultIfEmpty(0).Sum();
            var totalTickets = accounts.Select(a => a.Tickets).DefaultIfEmpty(0).Sum();

            var todayDateTime = DateTime.Today;

            var dailyRewardsToday = dailyRewardRepository.GetAll().Where(dr => dr.CreatedAt >= todayDateTime).DistinctBy(dr => dr.AccountId);
            int doneCount         = dailyRewardsToday.Count(),
                wholeCount        = accounts.Count(acc => !string.IsNullOrEmpty(acc.ProviderToken));

            var todayEarnings    = earningRepository.GetAll().Where(earning => earning.Created > todayDateTime);
            var todayEarningsSum = todayEarnings.Select(earning => earning.Total).DefaultIfEmpty(0).Sum();

            return $"<b>CZ time:</b> <code>{DateTime.UtcNow.AddHours(2):dd.MM.yyyy HH:mm:ss}</code>\n" +
                   $"<b>MSK time:</b> <code>{DateTime.UtcNow.AddHours(3):dd.MM.yyyy HH:mm:ss}</code>\n" +
                   $"Total accounts: <b>{accounts.Count()}</b>\n" +
                   $"Total balance: <b>{totalBalance:N2}</b> ฿\n" +
                   $"Total tickets: <b>{totalTickets}</b>\n" +
                   $"Daily rewards today: <b>{doneCount}</b>/<b>{wholeCount}</b>\n" +
                   $"Today earnings: ≈<b>{todayEarningsSum:N2}</b> ฿";
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Log.Error($"Telegram Bot HandleErrorAsync: {exception}");
            return Task.CompletedTask;
        }
    }
}
