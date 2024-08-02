using BlumBotFarm.Database.Repositories;
using BlumBotFarm.GameClient;
using Quartz;
using Serilog;
using System.Text;
using Task = System.Threading.Tasks.Task;

namespace BlumBotFarm.Scheduler.Jobs
{
    public class UpdateUsersInfoJob : IJob
    {
        public static readonly int MIN_MINUTES_TO_WAIT = 30, MAX_MINUTES_TO_WAIT = 60;

        private static readonly int MIN_MS_TO_WAIT_BETWEEN_REQUESTS = 100, MAX_MS_TO_WAIT_BETWEEN_REQUESTS = 1000;

        private readonly AccountRepository accountRepository;
        private readonly TaskRepository    taskRepository;
        private readonly TaskScheduler     taskScheduler;

        public UpdateUsersInfoJob()
        {
            var db            = Database.Database.ConnectionString;
            accountRepository = new AccountRepository(db);
            taskRepository    = new TaskRepository(db);
            taskScheduler     = new TaskScheduler();
        }

        public async Task Execute(IJobExecutionContext context)
        {
            GameApiClient gameApiClient = new();

            Random random = new();
            var accounts  = accountRepository.GetAll().OrderBy(_ => random.Next()).ToList();

            List<string> messageLines = ["Update Users Info:"];
            foreach (var accountForeach in accounts)
            {
                // Auth check, first of all
                ApiResponse authCheckResult = ApiResponse.Error;
                if ((authCheckResult = GameApiUtilsService.AuthCheck(accountForeach, accountRepository, gameApiClient)) != ApiResponse.Success)
                {
                    Log.Error($"Update Users Info Job, GameApiUtilsService.AuthCheck: UNABLE TO REAUTH! Account with Id: {accountForeach.Id}, " +
                              $"CustomUsername: {accountForeach.CustomUsername}, BlumUsername: {accountForeach.BlumUsername}.");
                }
                else
                {
                    var account = accountRepository.GetById(accountForeach.Id);
                    if (account == null)
                    {
                        Log.Error("Update Users Info Job, the Account is NULL from DB after reauth.");
                        continue;
                    }

                    (var getUserInfoResult, var balance, var tickets) = gameApiClient.GetUserInfo(account);
                    if (getUserInfoResult == ApiResponse.Success)
                    {
                        string balanceChangedString = Math.Abs(account.Balance - balance) < 1e-5 ? "" : $" => <b>{balance}</b> ฿";
                        string ticketsChangedString = account.Tickets == tickets ? "" : $" => <b>{tickets}</b>";
                        messageLines.Add($"(<code>{account.CustomUsername}</code>, Blum <code>{account.BlumUsername}</code>): " +
                                         $"<b>{account.Balance}</b> ฿{balanceChangedString}, " +
                                         $"<b>{account.Tickets}</b>{ticketsChangedString} tickets");

                        account.Balance = balance;
                        account.Tickets = tickets;
                        accountRepository.Update(account);
                        Log.Information($"Update Users Info Job, balance is {balance}, ticket's count is {tickets} for an account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");
                    }
                    else
                    {
                        messageLines.Add($"(<code>{account.CustomUsername}</code>, Blum <code>{account.BlumUsername}</code>): cannot get the info");

                        Log.Error($"Update Users Info Job, error while getting user info for an account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");
                    }

                    int msToWait = random.Next(MIN_MS_TO_WAIT_BETWEEN_REQUESTS, MAX_MS_TO_WAIT_BETWEEN_REQUESTS + 1);
                    Thread.Sleep(msToWait);
                }
            }

            StringBuilder messageToSend = new();
            foreach (var line in messageLines)
            {
                messageToSend.AppendLine(line);

                if (messageToSend.Length > 2048) // The max TG message length is 4096
                {
                    MessageProcessor.MessageProcessor.Instance?.SendMessageToAdminsInQueue(messageToSend.ToString(), isSilent: false);

                    messageToSend.Clear();
                }
            }
            MessageProcessor.MessageProcessor.Instance?.SendMessageToAdminsInQueue(messageToSend.ToString(), isSilent: false);
        }
    }
}
