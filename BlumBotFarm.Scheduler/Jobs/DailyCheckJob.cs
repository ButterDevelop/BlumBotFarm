using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Repositories;
using BlumBotFarm.GameClient;
using Quartz;
using Serilog;
using Task = BlumBotFarm.Core.Models.Task;

namespace BlumBotFarm.Scheduler.Jobs
{
    public class DailyCheckJob : IJob
    {
        private readonly GameApiClient     gameApiClient;
        private readonly AccountRepository accountRepository;
        private readonly TaskRepository    taskRepository;
        private readonly TaskScheduler     taskScheduler;

        public DailyCheckJob()
        {
            gameApiClient = new GameApiClient();
            using (var db = Database.Database.GetConnection())
            {
                accountRepository = new AccountRepository(db);
                taskRepository    = new TaskRepository(db);
            }
            taskScheduler = new TaskScheduler();
        }

        public async System.Threading.Tasks.Task Execute(IJobExecutionContext context)
        {
            var account   = (Account)context.MergedJobDataMap["account"];
            var task      = (Task)context.MergedJobDataMap["taskDailyCheckJob"];
            var isPlanned = (bool)context.MergedJobDataMap["isPlanned"];

            if (account is null || task == null)
            {
                Log.Warning($"Exiting Daily Check Job because of: Account is " + (account is null ? "NULL" : "NOT NULL") +
                                                                  ", Task is " + (task    is null ? "NULL" : "NOT NULL"));
                return;
            }

            account = accountRepository.GetById(account.Id);
            if (account == null)
            {
                Log.Warning("Exiting Daily Check Job because of: Account is NULL after getting it from the Database.");
                return;
            }

            Log.Information($"Started Daily Check Job for an account with Id: {account.Id}, Username: {account.Username}");

            Random random = new();
            Thread.Sleep(random.Next(TaskScheduler.MIN_MS_AMOUNT_TO_WAIT_BEFORE_JOB, TaskScheduler.MAX_MS_AMOUNT_TO_WAIT_BEFORE_JOB + 1));

            // Auth check, first of all
            if (!GameApiUtilsService.AuthCheck(ref account, accountRepository, gameApiClient)) return;

            // Updating user info (maybe auth changed)
            accountRepository.Update(account);

            // Doing Daily Reward Job
            if (gameApiClient.GetDailyReward(account) != ApiResponse.Success)
            {
                Log.Information($"Can't take daily reward for some reason for an account with Id: {account.Id}, Username: {account.Username}.");
            }

            // Starting and claiming all the tasks
            GameApiUtilsService.StartAndClaimAllTasks(account, gameApiClient);

            // Claiming our possible reward for friends
            var friendsClaimResponse = gameApiClient.ClaimFriends(account);
            if (friendsClaimResponse != ApiResponse.Success)
            {
                Log.Information($"Can't take friends reward for some reason for an account with Id: {account.Id}, Username: {account.Username}.");
            }

            // Updating user info
            (ApiResponse result, double balance, int tickets) = gameApiClient.GetUserInfo(account);
            account.Balance = balance;
            account.Tickets = tickets;
            accountRepository.Update(account);

            // Playing games with all the tickets
            GameApiUtilsService.PlayGamesForAllTickets(ref account, accountRepository, gameApiClient);

            if (isPlanned)
            {
                // Determine the next run time based on the result
                DateTime nextRunTime;
                if (result == ApiResponse.Success)
                {
                    nextRunTime = DateTime.Now.AddHours(24); // Запланировать задание снова через 24 часа
                    Log.Information($"Daily Check Job is planned to be executed in 24 hours as usual for an account with Id: {account.Id}, Username: {account.Username}.");
                }
                else
                {
                    nextRunTime = DateTime.Now.AddHours(1); // Запланировать задание снова через 1 час
                    Log.Warning($"Daily Check Job is planned to be executed in 1 hour because of not successful server's answer for an account with Id: {account.Id}, Username: {account.Username}.");
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
