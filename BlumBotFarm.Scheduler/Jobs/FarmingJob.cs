using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Repositories;
using BlumBotFarm.GameClient;
using Quartz;
using Serilog;
using Task = BlumBotFarm.Core.Models.Task;

namespace BlumBotFarm.Scheduler.Jobs
{
    public class FarmingJob : IJob
    {
        private readonly GameApiClient     gameApiClient;
        private readonly AccountRepository accountRepository;
        private readonly TaskRepository    taskRepository;
        private readonly TaskScheduler     taskScheduler;
        
        public FarmingJob()
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
            var task      = (Task)context.MergedJobDataMap["taskFarming"];
            var isPlanned = (bool)context.MergedJobDataMap["isPlanned"];

            if (account is null || task == null)
            {
                Log.Warning($"Exiting Farming Job because of: Account is " + (account is null ? "NULL" : "NOT NULL") + 
                                                              ", Task is " + (task    is null ? "NULL" : "NOT NULL"));
                return;
            }

            account = accountRepository.GetById(account.Id);
            if (account == null) 
            {
                Log.Warning("Exiting Farming Job because of: Account is NULL after getting it from the Database.");
                return;
            }

            Log.Information($"Started Farming Job for an account with Id: {account.Id}, Username: {account.Username}");

            Random random = new();
            Thread.Sleep(random.Next(TaskScheduler.MIN_MS_AMOUNT_TO_WAIT_BEFORE_JOB, TaskScheduler.MAX_MS_AMOUNT_TO_WAIT_BEFORE_JOB + 1));

            // Auth check, first of all
            if (!GameApiUtilsService.AuthCheck(ref account, accountRepository, gameApiClient)) return;

            // Updating user info (maybe auth changed)
            accountRepository.Update(account);

            // Doing claiming a farming stuff
            (ApiResponse claimResponse, double balance, int tickets) = gameApiClient.ClaimFarming(account);
            if (claimResponse == ApiResponse.Success)
            {
                // Updating user info
                account.Balance = balance;
                account.Tickets = tickets;
                accountRepository.Update(account);
            }

            // Doing starting farming stuff
            var startFarmingResponse = gameApiClient.StartFarming(account);

            if (isPlanned)
            {
                DateTime nextRunTime;
                if (startFarmingResponse == ApiResponse.Success)
                {
                    // Запланировать задание снова через 8 часов
                    nextRunTime = DateTime.Now.AddHours(8);
                    Log.Information($"Farming Job is planned to be executed in 8 hours as usual for an account with Id: {account.Id}, Username: {account.Username}.");
                }
                else
                {
                    // Запланировать задание снова через 30 минут
                    nextRunTime = DateTime.Now.AddMinutes(30);
                    Log.Warning($"Farming Job is planned to be executed in 30 minutes because of not successful server's answer for an account with Id: {account.Id}, Username: {account.Username}.");
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

            Log.Information($"Farming Job is done for an account with Id: {account.Id}, Username: {account.Username}");
        }
    }
}
