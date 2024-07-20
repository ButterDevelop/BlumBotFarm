using BlumBotFarm.Database.Repositories;
using Quartz;
using Serilog;

namespace BlumBotFarm.Scheduler.Jobs
{
    public class MainSchedulerJob : IJob
    {
        private readonly DateTime START_DATE_TIME = new(2024, 1, 1, 6, 0, 0),
                                  END_DATE_TIME   = new(2024, 1, 1, 18, 0, 0);

        private readonly AccountRepository accountRepository;
        private readonly TaskRepository    taskRepository;
        private readonly TaskScheduler     taskScheduler;

        public MainSchedulerJob()
        {
            using (var db = Database.Database.GetConnection())
            {
                accountRepository = new AccountRepository(db);
                taskRepository    = new TaskRepository(db);
            }
            taskScheduler = new TaskScheduler();
        }

        public async Task Execute(IJobExecutionContext context)
        {
            // Получение всех аккаунтов из базы данных и случайное их перемешивание
            Random random = new();
            var accounts  = accountRepository.GetAll().OrderBy(acc => acc.Id).ToList();

            var tasks = taskRepository.GetAll();

            if (!tasks.Any())
            {
                Log.Error("Main Scheduler Job, No tasks available!");
                return;
            }

            // Delete all tasks before adding new
            await taskScheduler.DeleteAllTasks();

            TimeSpan interval = TimeSpan.FromTicks((END_DATE_TIME - START_DATE_TIME).Ticks / tasks.Count());
            DateTime startAt  = DateTime.Today.Date.Add(START_DATE_TIME.TimeOfDay);

            List<string> logLines = [];
            foreach (var account in accounts)
            {
                if (string.IsNullOrEmpty(account.ProviderToken)) continue;

                // Восстановление задачи из базы данных
                var tasksForAccount = tasks.Where(t => t.AccountId == account.Id);
                if (!tasksForAccount.Any())
                {
                    Log.Warning($"Did not found tasks in DB for an account {account.Id}.");
                    continue;
                }

                foreach (var task in tasksForAccount)
                {
                    int msToAdd = random.Next(TaskScheduler.MIN_MS_AMOUNT_TO_WAIT_BEFORE_JOB, TaskScheduler.MAX_MS_AMOUNT_TO_WAIT_BEFORE_JOB + 1);
                    var nextRun = startAt.AddMilliseconds(msToAdd);

                    // Update task in DB
                    task.NextRunTime = nextRun;
                    taskRepository.Update(task);

                    // Schedule task
                    await TaskScheduler.ScheduleNewTask(taskScheduler, account.Id, task, nextRun);

                    logLines.Add($"Schedule task - accountId: {account.Id}, accountCustomUsername: {account.CustomUsername}, " +
                                 $"accountBlumUsername: {account.BlumUsername}, taskId: {task.Id}, " +
                                 $"taskType: {task.TaskType}, time: {nextRun:dd.MM.yyyy HH:mm:ss}");

                    startAt = startAt.Add(interval);
                }
            }

            foreach (var line in logLines) Log.Information(line);
        }
    }
}
