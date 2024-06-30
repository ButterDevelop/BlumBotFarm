using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Repositories;
using BlumBotFarm.GameClient;
using Quartz;
using Serilog;

namespace BlumBotFarm.Scheduler.Jobs
{
    public class FarmingJob : IJob
    {
        private readonly GameApiClient     gameApiClient;
        private readonly AccountRepository accountRepository;
        private readonly TaskRepository    taskRepository;
        private readonly EarningRepository earningRepository;
        private readonly TaskScheduler     taskScheduler;
        
        public FarmingJob()
        {
            gameApiClient = new GameApiClient();
            using (var db = Database.Database.GetConnection())
            {
                accountRepository = new AccountRepository(db);
                taskRepository    = new TaskRepository(db);
                earningRepository = new EarningRepository(db);
            }
            taskScheduler = new TaskScheduler();
        }

        public async System.Threading.Tasks.Task Execute(IJobExecutionContext context)
        {
            var accountId = (int)context.MergedJobDataMap["accountId"];
            var taskId    = (int)context.MergedJobDataMap["taskIdFarming"];
            var isPlanned = (bool)context.MergedJobDataMap["isPlanned"];

            var account = accountRepository.GetById(accountId);
            var task    = taskRepository.GetById(taskId);

            if (account is null || (task == null && isPlanned))
            {
                Log.Error($"Exiting Farming Job because of: Account is " + (account is null ? "NULL" : "NOT NULL") + 
                                                            ", Task is " + (task    is null ? "NULL" : "NOT NULL") +
                                                            " after getting it from the Database.");
                return;
            }

            Log.Information($"Started Farming Job for an account with Id: {account.Id}, Username: {account.Username}");

            Random random = new();
            Thread.Sleep(random.Next(TaskScheduler.MIN_MS_AMOUNT_TO_WAIT_BEFORE_JOB, TaskScheduler.MAX_MS_AMOUNT_TO_WAIT_BEFORE_JOB + 1));

            // Getting the frontend part
            if (!gameApiClient.GetMainPageHTML(account))
            {
                Log.Warning($"Farming Job, GetMainPageHTML: returned FALSE for an account with Id: {account.Id}, Username: {account.Username}.");
            }

            ApiResponse startFarmingResponse = ApiResponse.Error;
            bool isAuthGood = false;

            // Auth check, first of all
            if (!GameApiUtilsService.AuthCheck(account, accountRepository, gameApiClient))
            {
                Log.Error($"Farming Job, GameApiUtilsService.AuthCheck: UNABLE TO REAUTH! Account with Id: {account.Id}, Username: {account.Username}.");
                MessageProcessor.MessageProcessor.Instance.SendMessageToAdminsInQueue("<b>UNABLE TO REAUTH!</b>\nFarming Job!\n" +
                                                                                      $"Account with Id: <code>{account.Id}</code>, Username: <code>{account.Username}</code>");
                isAuthGood = false;
            }
            else
            {
                Log.Information($"Farming Job, Auth is good for an account with Id: {account.Id}, Username: {account.Username}.");

                isAuthGood = true;

                account = accountRepository.GetById(accountId);
                if (account == null)
                {
                    Log.Error("Farming Job, the Account is NULL from DB after reauth.");
                    return;
                }

                // Doing claiming a farming stuff
                (ApiResponse claimResponse, double balance, int tickets) = gameApiClient.ClaimFarming(account);
                if (claimResponse == ApiResponse.Success)
                {
                    Log.Information($"Farming Job, claimed farming successfully for an account Id: {account.Id}, Username: {account.Username}.");

                    if (balance - account.Balance > 0)
                    {
                        earningRepository.Add(new Earning
                        {
                            AccountId = account.Id,
                            Created   = DateTime.Now,
                            Action    = "ClaimFarming",
                            Total     = balance - account.Balance,
                        });

                        Log.Information($"Farming Job, added earning with total {balance - account.Balance} for an account Id: {account.Id}, Username: {account.Username}");
                    }
                    else
                    {
                        Log.Warning($"Farming Job, earning with total {balance - account.Balance} is negative for an account Id: {account.Id}, Username: {account.Username}");
                    }

                    // Updating user info
                    account.Balance = balance;
                    account.Tickets = tickets;
                    accountRepository.Update(account);
                }
                else
                {
                    Log.Error($"Farming Job, error while claiming farming for an account Id: {account.Id}, Username: {account.Username}.");
                }

                // Doing starting farming stuff
                startFarmingResponse = gameApiClient.StartFarming(account);
                if (startFarmingResponse == ApiResponse.Success)
                {
                    Log.Information($"Farming Job, started farming successfully for an account Id: {account.Id}, Username: {account.Username}.");
                }
                else
                {
                    Log.Error($"Farming Job, error while starting farming for an account Id: {account.Id}, Username: {account.Username}.");
                }

                if (isPlanned && task != null)
                {
                    Log.Information($"Farming Job, planning future Jobs for an account Id: {account.Id}, Username: {account.Username}.");

                    DateTime nextRunTime;
                    if (startFarmingResponse == ApiResponse.Success && isAuthGood)
                    {
                        // Запланировать задание снова через 8 часов
                        nextRunTime = DateTime.Now.AddHours(8);
                        Log.Information("Farming Job is planned to be executed in 8 hours as usual for an account with Id: " +
                                        $"{account.Id}, Username: {account.Username}.");
                    }
                    else
                    {
                        // Запланировать задание снова через 30 минут
                        nextRunTime = DateTime.Now.AddMinutes(30);
                        Log.Warning("Farming Job is planned to be executed in 30 minutes because of not successful server's answer " +
                                    $"for an account with Id: {account.Id}, Username: {account.Username}.");
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

            Log.Information($"Farming Job is done for an account with Id: {account.Id}, Username: {account.Username}");
        }
    }
}
