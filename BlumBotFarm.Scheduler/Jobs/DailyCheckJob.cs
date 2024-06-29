using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Repositories;
using BlumBotFarm.GameClient;
using Quartz;
using Serilog;

namespace BlumBotFarm.Scheduler.Jobs
{
    public class DailyCheckJob : IJob
    {
        private readonly GameApiClient     gameApiClient;
        private readonly AccountRepository accountRepository;
        private readonly TaskRepository    taskRepository;
        private readonly MessageRepository messageRepository;
        private readonly EarningRepository earningRepository;
        private readonly TaskScheduler     taskScheduler;

        public DailyCheckJob()
        {
            gameApiClient = new GameApiClient();
            using (var db = Database.Database.GetConnection())
            {
                accountRepository = new AccountRepository(db);
                taskRepository    = new TaskRepository(db);
                messageRepository = new MessageRepository(db);
                earningRepository = new EarningRepository(db);
            }
            taskScheduler = new TaskScheduler();
        }

        public async System.Threading.Tasks.Task Execute(IJobExecutionContext context)
        {
            var accountId = (int)context.MergedJobDataMap["accountId"];
            var taskId    = (int)context.MergedJobDataMap["taskIdDailyCheckJob"];
            var isPlanned = (bool)context.MergedJobDataMap["isPlanned"];

            var account = accountRepository.GetById(accountId);
            var task    = taskRepository.GetById(taskId);

            if (account is null || (task == null && isPlanned))
            {
                Log.Error($"Exiting Daily Check Job because of: Account is " + (account is null ? "NULL" : "NOT NULL") +
                                                                ", Task is " + (task    is null ? "NULL" : "NOT NULL") +
                                                                " after getting it from the Database.");
                return;
            }

            Log.Information($"Started Daily Check Job for an account with Id: {account.Id}, Username: {account.Username}");

            Random random = new();
            Thread.Sleep(random.Next(TaskScheduler.MIN_MS_AMOUNT_TO_WAIT_BEFORE_JOB, TaskScheduler.MAX_MS_AMOUNT_TO_WAIT_BEFORE_JOB + 1));

            // Getting the frontend part
            if (!gameApiClient.GetMainPageHTML(account))
            {
                Log.Warning($"Daily Check Job, GetMainPageHTML: returned FALSE for an account with Id: {account.Id}, Username: {account.Username}.");
            }
            
            ApiResponse dailyClaimResponse = ApiResponse.Error, friendsClaimResponse = ApiResponse.Error, getUserInfoResult = ApiResponse.Error;
            bool startAndClaimAllTasksIsGood = false, isAuthGood = false;

            // Auth check, first of all
            if (!GameApiUtilsService.AuthCheck(account, accountRepository, gameApiClient))
            {
                Log.Error($"Daily Check Job, GameApiUtilsService.AuthCheck: UNABLE TO REAUTH! Account with Id: {account.Id}, Username: {account.Username}.");
                MessageProcessor.MessageProcessor.Instance.SendMessageToAdminsInQueue("<b>UNABLE TO REAUTH!</b>\nDaily Check Job!\n" +
                                                                                      $"Account with Id: <code>{account.Id}</code>, Username: <code>{account.Username}</code>");
                isAuthGood = false;
            }
            else
            {
                Log.Information($"Daily Check Job, Auth is good for an account with Id: {account.Id}, Username: {account.Username}.");

                isAuthGood = true;

                account = accountRepository.GetById(accountId);
                if (account == null)
                {
                    Log.Error("Daily Check Job, the Account is NULL from DB after reauth.");
                    return;
                }

                // Updating user info
                (getUserInfoResult, var balance1, var tickets) = gameApiClient.GetUserInfo(account);
                if (getUserInfoResult == ApiResponse.Success)
                {
                    account.Balance = balance1;
                    account.Tickets = tickets;
                    accountRepository.Update(account);
                    Log.Information($"Daily Check Job, balance is {balance1}, ticket's count is {tickets} for an account with Id: {account.Id}, Username: {account.Username}.");
                }
                else
                {
                    Log.Error($"Daily Check Job, error while getting user info for an account with Id: {account.Id}, Username: {account.Username}.");
                }

                // Doing Daily Reward Job
                dailyClaimResponse = gameApiClient.GetDailyReward(account);
                if (dailyClaimResponse != ApiResponse.Success)
                {
                    Log.Information($"Daily Check Job, can't take daily reward for some reason for an account with Id: {account.Id}, Username: {account.Username}.");
                }
                else
                {
                    Log.Information($"Daily Check Job, ended working with daily reward for an account Id: {account.Id}, Username: {account.Username}.");
                }

                // Starting and claiming all the tasks
                startAndClaimAllTasksIsGood = GameApiUtilsService.StartAndClaimAllTasks(account, earningRepository, gameApiClient);
                Log.Information($"Daily Check Job, ended working with tasks for an account Id: {account.Id}, Username: {account.Username}.");

                // Claiming our possible reward for friends
                friendsClaimResponse = gameApiClient.ClaimFriends(account);
                if (friendsClaimResponse != ApiResponse.Success)
                {
                    Log.Information($"Daily Check Job, can't take friends reward for some reason for an account with Id: {account.Id}, Username: {account.Username}.");
                }
                else
                {
                    Log.Information($"Daily Check Job, ended working with friends reward for an account Id: {account.Id}, Username: {account.Username}.");
                }

                // Playing games with all the tickets
                GameApiUtilsService.PlayGamesForAllTickets(account, accountRepository, earningRepository, gameApiClient);
                Log.Information($"Daily Check Job, ended playing for all tickets for an account Id: {account.Id}, Username: {account.Username}, " +
                                $"Balance: {account.Balance}, Tickets: {account.Tickets}.");

                account = accountRepository.GetById(accountId);
                if (account == null)
                {
                    Log.Error("Daily Check Job, the Account is NULL from DB after playing for all tickets.");
                    return;
                }

                if (getUserInfoResult == ApiResponse.Success)
                {
                    var total = account.Balance - balance1;

                    earningRepository.Add(new Earning
                    {
                        AccountId = account.Id,
                        Created   = DateTime.Now,
                        Action    = "GetUserInfo",
                        Total     = total,
                    });

                    Log.Information($"Daily Check Job, added earning with total {total} for an account Id: {account.Id}, Username: {account.Username}");
                }
            }

            if (isPlanned && task != null)
            {
                Log.Information($"Daily Check Job, planning future Jobs for an account Id: {account.Id}, Username: {account.Username}.");

                // Determine the next run time based on the result
                DateTime nextRunTime;
                if (getUserInfoResult == ApiResponse.Success && dailyClaimResponse == ApiResponse.Success && 
                    account.Tickets == 0 && startAndClaimAllTasksIsGood && isAuthGood)
                {
                    nextRunTime = DateTime.Now.AddHours(24); // Запланировать задание снова через 24 часа
                    Log.Information("Daily Check Job is planned to be executed in 24 hours as usual for an account with Id: " +
                                    $"{account.Id}, Username: {account.Username}.");
                }
                else
                {
                    nextRunTime = DateTime.Now.AddHours(1); // Запланировать задание снова через 1 час
                    Log.Warning($"Daily Check Job is planned to be executed in 1 hour because of not successful server's answer or tickets amount " +
                                $"not equals zero for an account with Id: {account.Id}, Username: {account.Username}, Tickets Amount: {account.Tickets}.");
                }

                // Update the existing trigger with the new schedule
                var trigger = context.Trigger as ICronTrigger;
                if (trigger != null)
                {
                    trigger = (ICronTrigger)TriggerBuilder.Create()
                        .WithIdentity(context.Trigger.Key)
                        .WithSimpleSchedule(schedule => schedule
                                        .WithIntervalInSeconds(task.ScheduleSeconds)
                                        .RepeatForever())
                        .StartAt(nextRunTime)
                        .Build();

                    await context.Scheduler.RescheduleJob(trigger.Key, trigger);
                }

                // Обновление записи задачи в базе данных
                task.NextRunTime = nextRunTime;
                taskRepository.Update(task);
            }

            Log.Information($"Daily Check Job is done for an account with Id: {account.Id}, Username: {account.Username}");
        }
    }
}
