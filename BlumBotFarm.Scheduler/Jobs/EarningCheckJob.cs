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
        private readonly TaskRepository    taskRepository;
        private readonly EarningRepository earningRepository;
        private readonly TaskScheduler     taskScheduler;

        public EarningCheckJob()
        {
            var db = Database.Database.ConnectionString;
            accountRepository = new AccountRepository(db);
            taskRepository    = new TaskRepository(db);
            earningRepository = new EarningRepository(db);
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

            // Auth check, first of all
            ApiResponse authCheckResult = ApiResponse.Error;
            if ((authCheckResult = GameApiUtilsService.AuthCheck(account, accountRepository, gameApiClient)) != ApiResponse.Success)
            {
                Log.Error($"Earning Check Job, GameApiUtilsService.AuthCheck: UNABLE TO REAUTH! Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");
                if (authCheckResult == ApiResponse.Unauthorized)
                {
                    MessageProcessor.MessageProcessor.Instance?.SendMessageToAdminsInQueue(
                        "<b>UNABLE TO REAUTH!</b>\nEarning Check Job!\n" +
                        $"Account with Id: <code>{account.Id}</code>, " +
                        $"Custom Username: <code>{account.CustomUsername}</code>, " +
                        $"Blum Username: <code>{account.BlumUsername}</code>",
                        isSilent: false
                    );
                }
            }
            else
            {
                account = accountRepository.GetById(account.Id);
                if (account == null)
                {
                    Log.Error("Earning Check Job, the Account is NULL from DB after reauth.");
                    return;
                }

                // Updating user info
                (var getUserInfoResult, var gotBalance, var tickets) = gameApiClient.GetUserInfo(account);
                if (getUserInfoResult == ApiResponse.Success)
                {
                    var earning = new Earning
                    {
                        AccountId = accountId,
                        Action    = type,
                        Created   = DateTime.Now,
                        Total     = gotBalance - balance,
                    };
                    earningRepository.Add(earning);

                    account.Balance = gotBalance;
                    account.Tickets = tickets;
                    accountRepository.Update(account);

                    Log.Information($"Earning Check Job, balance is {gotBalance}, ticket's count is {tickets} for an account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");

                    if (tickets > 0)
                    {
                        Log.Information($"Earning Check Job, ticket's count is {tickets}, more than 0, for an account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}. Starting Daily Check Job again.");

                        var task = taskRepository.GetAll().FirstOrDefault(t => t.AccountId == accountId && t.TaskType == "DailyCheckJob");
                        if (task is null)
                        {
                            Log.Error($"Earning Check Job, can't get the DailyCheckJob task from DB for an account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");
                        }
                    }
                }
                else
                {
                    Random random = new();
                    var startDate = DateTime.Now.AddMinutes(random.Next(MIN_MINUTES_TO_WAIT, MAX_MINUTES_TO_WAIT + 1));
                    await TaskScheduler.ScheduleEarningJob(taskScheduler, accountId, balance, type, startDate);

                    Log.Error($"Earning Check Job, error while getting user info for an account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}. Scheduled new one.");
                }
            }
        }
    }
}
