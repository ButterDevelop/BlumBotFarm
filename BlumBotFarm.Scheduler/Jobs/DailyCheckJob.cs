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
            Random random = new();
            Thread.Sleep(random.Next(TaskScheduler.MIN_MS_AMOUNT_TO_WAIT_BEFORE_JOB, TaskScheduler.MAX_MS_AMOUNT_TO_WAIT_BEFORE_JOB + 1));

            var account   = (Account)context.MergedJobDataMap["account"];
            var task      = (Task)context.MergedJobDataMap["taskDailyCheckJob"];
            var isPlanned = (bool)context.MergedJobDataMap["isPlanned"];

            if (account is null || task == null) return;

            // Auth check, first of all
            if (!GameApiUtilsService.AuthCheck(ref account, accountRepository, gameApiClient)) return;

            // Doing Daily Reward Job
            if (gameApiClient.GetDailyReward(account) != ApiResponse.Success)
            {
                Log.Information("Can't take daily reward for some reason.");
            }

            // Starting and claiming all the tasks
            GameApiUtilsService.StartAndClaimAllTasks(account, gameApiClient);

            // Claiming our possible reward for friends
            var friendsClaimResponse = gameApiClient.ClaimFriends(account);

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
                }
                else
                {
                    nextRunTime = DateTime.Now.AddHours(1); // Запланировать задание снова через 1 час
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
        }
    }
}
