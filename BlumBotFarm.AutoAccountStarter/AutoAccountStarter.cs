using BlumBotFarm.Core;
using BlumBotFarm.Database.Repositories;
using Serilog;
using TaskScheduler = BlumBotFarm.Scheduler.TaskScheduler;

namespace BlumBotFarm.AutoAccountStarter
{
    public class AutoAccountStarter
    {
        private readonly CancellationToken _cancellationToken;
        private readonly AccountRepository _accountRepository;
        private readonly TaskRepository    _taskRepository;
        private readonly TaskScheduler     _taskScheduler;
        private HashSet<int> _workingAccounts;

        public AutoAccountStarter()
        {
            _cancellationToken = new CancellationToken();

            var dbConnectionString = AppConfig.DatabaseSettings.MONGO_CONNECTION_STRING;
            var databaseName       = AppConfig.DatabaseSettings.MONGO_DATABASE_NAME;

            _accountRepository = new AccountRepository(dbConnectionString, databaseName, AppConfig.DatabaseSettings.MONGO_ACCOUNT_PATH);
            _taskRepository    = new TaskRepository(dbConnectionString, databaseName, AppConfig.DatabaseSettings.MONGO_TASK_PATH);
            _taskScheduler     = new();
            _workingAccounts   = [];
        }

        public AutoAccountStarter(CancellationToken cancellationToken) : this()
        {
            _cancellationToken = cancellationToken;
            foreach (var account in _accountRepository.GetAll())
            {
                if (!string.IsNullOrEmpty(account.ProviderToken) && !string.IsNullOrEmpty(account.RefreshToken) &&
                    !_workingAccounts.Contains(account.Id))
                {
                    _workingAccounts.Add(account.Id);
                }
            }
        }

        public async Task StartAsync()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                await ProcessAsync();
                await Task.Delay(60000); // Ждем 60 секунд перед следующей проверкой
            }
        }

        private async Task ProcessAsync()
        {
            var accounts = _accountRepository.GetAll();
            var tasks    = _taskRepository.GetAll();
            Random random = new();

            foreach (var account in accounts)
            {
                if (!_workingAccounts.Contains(account.Id) && !string.IsNullOrEmpty(account.ProviderToken))
                {
                    var task = tasks.FirstOrDefault(t => t.AccountId == account.Id && t.TaskType == "DailyCheckJob");
                    if (task == null) continue;

                    // Update task in DB
                    var nextRun = DateTime.Now.AddSeconds(task.TaskType.Length);
                    task.NextRunTime = nextRun;
                    _taskRepository.Update(task);
                    
                    // Schedule task
                    await TaskScheduler.ScheduleNewTask(_taskScheduler, account.Id, task, nextRun);
                    
                    _workingAccounts.Add(account.Id);

                    Log.Information($"Auto Account Starter: successfully scheduled job for account - accountId: {account.Id}, " +
                                    $"accountCustomUsername: {account.CustomUsername}, accountBlumUsername: {account.BlumUsername}, " +
                                    $"taskId: {task.Id}, taskType: {task.TaskType}, time: {nextRun:dd.MM.yyyy HH:mm:ss}");
                }
            }
        }
    }
}
