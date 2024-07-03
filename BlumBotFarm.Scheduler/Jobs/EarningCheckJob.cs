using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Repositories;
using BlumBotFarm.GameClient;
using Quartz;
using Serilog;
using Task = System.Threading.Tasks.Task;

namespace BlumBotFarm.Scheduler.Jobs
{
    public class EarningCheckJob : IJob
    {
        public static readonly int MIN_MINUTES_TO_WAIT = 5, MAX_MINUTES_TO_WAIT = 10;

        private readonly AccountRepository accountRepository;
        private readonly EarningRepository earningRepository;
        private readonly TaskScheduler     taskScheduler;

        public EarningCheckJob()
        {
            using (var db = Database.Database.GetConnection())
            {
                accountRepository = new AccountRepository(db);
                earningRepository = new EarningRepository(db);
            }
            taskScheduler = new TaskScheduler();
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var accountId = (int)context.MergedJobDataMap["accountId"];
            var balance   = (double)context.MergedJobDataMap["balance"];
            var type      = (string)context.MergedJobDataMap["type"];

            var account = accountRepository.GetById(accountId);

            if (account is null || type is null)
            {
                Log.Error($"Earning Check Job returned because of: Account is " + (account is null ? "NULL" : "NOT NULL") +
                                                                ", Type is "    + (type is null    ? "NULL" : "NOT NULL"));
                return;
            }

            GameApiClient gameApiClient = new();
            // Updating user info
            (var getUserInfoResult, var gotBalance, var tickets) = gameApiClient.GetUserInfo(account);
            if (getUserInfoResult == ApiResponse.Success)
            {
                var earning = new Earning
                {
                    AccountId = accountId,
                    Action    = type,
                    Created   = DateTime.Now,
                    Total     = gotBalance - account.Balance,
                };
                earningRepository.Add(earning);

                account.Balance = gotBalance;
                account.Tickets = tickets;
                accountRepository.Update(account);

                Log.Information($"Earning Check Job, balance is {gotBalance}, ticket's count is {tickets} for an account with Id: {account.Id}, Username: {account.Username}.");
            }
            else
            {
                Random random = new();
                var startDate = DateTime.Now.AddMinutes(random.Next(MIN_MINUTES_TO_WAIT, MAX_MINUTES_TO_WAIT + 1));
                await TaskScheduler.ScheduleEarningJob(taskScheduler, accountId, balance, type, startDate);

                Log.Error($"Earning Check Job, error while getting user info for an account with Id: {account.Id}, Username: {account.Username}. Scheduled new one.");
            }
        }
    }
}
