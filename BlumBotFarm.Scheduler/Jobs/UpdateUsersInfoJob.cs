using BlumBotFarm.Database.Repositories;
using BlumBotFarm.GameClient;
using Quartz;
using Serilog;

namespace BlumBotFarm.Scheduler.Jobs
{
    public class UpdateUsersInfoJob : IJob
    {
        public static readonly int MIN_MINUTES_TO_WAIT = 30, MAX_MINUTES_TO_WAIT = 60;

        private static readonly int MIN_SECONDS_TO_WAIT_BETWEEN_REQUESTS = 1, MAX_SECONDS_TO_WAIT_BETWEEN_REQUESTS = 3;

        private readonly AccountRepository accountRepository;
        private readonly TaskRepository    taskRepository;
        private readonly TaskScheduler     taskScheduler;

        public UpdateUsersInfoJob()
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
            GameApiClient gameApiClient = new();

            Random random = new();
            var accounts  = accountRepository.GetAll().OrderBy(_ => random.Next()).ToList();

            foreach (var account in accounts)
            {
                bool getMainPageResult = gameApiClient.GetMainPageHTML(account);

                (var getUserInfoResult, var balance, var tickets) = gameApiClient.GetUserInfo(account);
                if (getUserInfoResult == ApiResponse.Success)
                {
                    account.Balance = balance;
                    account.Tickets = tickets;
                    accountRepository.Update(account);
                    Log.Information($"Update Users Info Job, balance is {balance}, ticket's count is {tickets} for an account with Id: {account.Id}, Username: {account.Username}.");
                }
                else
                {
                    Log.Error($"Update Users Info Job, error while getting user info for an account with Id: {account.Id}, Username: {account.Username}.");
                }

                int secondsToWait = random.Next(MIN_SECONDS_TO_WAIT_BETWEEN_REQUESTS, MAX_SECONDS_TO_WAIT_BETWEEN_REQUESTS);
                Thread.Sleep(secondsToWait * 1000);
            }

            await TaskScheduler.ScheduleUpdateUsersInfo();
        }
    }
}
