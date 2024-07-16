using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Repositories;
using BlumBotFarm.GameClient;
using BlumBotFarm.Translation;
using Quartz;
using Serilog;
using System.Runtime.CompilerServices;

namespace BlumBotFarm.Scheduler.Jobs
{
    public class DailyCheckJob : IJob
    {
        private const string REFERRAL_LINK_PREFIX = "t.me/BlumCryptoBot/app?startapp=ref_";

        private readonly GameApiClient         gameApiClient;
        private readonly AccountRepository     accountRepository;
        private readonly TaskRepository        taskRepository;
        private readonly MessageRepository     messageRepository;
        private readonly EarningRepository     earningRepository;
        private readonly DailyRewardRepository dailyRewardRepository;
        private readonly UserRepository        userRepository;
        private readonly TaskScheduler         taskScheduler;

        public DailyCheckJob()
        {
            gameApiClient = new GameApiClient();
            using (var db = Database.Database.GetConnection())
            {
                accountRepository     = new AccountRepository(db);
                taskRepository        = new TaskRepository(db);
                messageRepository     = new MessageRepository(db);
                earningRepository     = new EarningRepository(db);
                dailyRewardRepository = new DailyRewardRepository(db);
                userRepository        = new UserRepository(db);
            }
            taskScheduler = new TaskScheduler();
        }

        public async System.Threading.Tasks.Task Execute(IJobExecutionContext context)
        {
            var accountId = (int)context.MergedJobDataMap["accountId"];
            var taskId    = (int)context.MergedJobDataMap["taskIdDailyCheckJob"];
            var isPlanned = (bool)context.MergedJobDataMap["isPlanned"];

            var account = accountRepository.GetById(accountId);
            var task    = taskRepository.GetById(taskId);

            if (account is null || (task == null && isPlanned))
            {
                Log.Error($"Exiting Daily Check Job because of: Account is " + (account is null ? "NULL" : "NOT NULL") +
                                                                ", Task is " + (task    is null ? "NULL" : "NOT NULL") +
                                                                " after getting it from the Database.");
                return;
            }

            if (string.IsNullOrEmpty(account.ProviderToken) && string.IsNullOrEmpty(account.RefreshToken))
            {
                Log.Warning($"Exiting Daily Check Job because of an account (Id: {account.Id}, CustomUsername: {account.CustomUsername}, " +
                            $"BlumUsername: {account.BlumUsername}) has no auth at all.");
                return;
            }

            var user = userRepository.GetById(account.UserId);
            if (user == null)
            {
                Log.Error($"Exiting Daily Check Job because user was NULL for an account (Id: {account.Id}, CustomUsername: {account.CustomUsername}, " +
                            $"BlumUsername: {account.BlumUsername}).");
                return;
            }

            Log.Information($"Started Daily Check Job for an account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, " +
                            $"BlumUsername: {account.BlumUsername}");

            Random random = new();
            //if (isPlanned)
            //{
            //    Thread.Sleep(random.Next(TaskScheduler.MIN_MS_AMOUNT_TO_WAIT_BEFORE_JOB, TaskScheduler.MAX_MS_AMOUNT_TO_WAIT_BEFORE_JOB + 1));
            //}

            // Getting the frontend part
            //if (!gameApiClient.GetMainPageHTML(account))
            //{
            //    Log.Warning($"Daily Check Job, GetMainPageHTML: returned FALSE for an account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");
            //}

            ApiResponse dailyClaimResponse = ApiResponse.Error, friendsClaimResponse = ApiResponse.Error, getUserInfoResult = ApiResponse.Error;
            bool startAndClaimAllTasksIsGood = false, isAuthGood = false;

            // Auth check, first of all
            ApiResponse authCheckResult = ApiResponse.Error;
            if ((authCheckResult = GameApiUtilsService.AuthCheck(account, accountRepository, gameApiClient)) != ApiResponse.Success)
            {
                Log.Error($"Daily Check Job, GameApiUtilsService.AuthCheck: UNABLE TO REAUTH! Account with Id: {account.Id}, " +
                          $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");

                MessageProcessor.MessageProcessor.Instance?.SendMessageToAdminsInQueue(
                    "<b>UNABLE TO REAUTH!</b>\nDaily Check Job!\n" +
                    $"Account with Id: <code>{account.Id}</code>, Custom Username: <code>{account.CustomUsername}</code>, " +
                    $"Blum Username: <code>{account.BlumUsername}</code>" +
                    (authCheckResult == ApiResponse.Error ? "\nIt is probably because of proxy." : ""),
                    isSilent: authCheckResult == ApiResponse.Error
                );

                if (authCheckResult == ApiResponse.Unauthorized)
                {
                    MessageProcessor.MessageProcessor.Instance?.SendMessageToUserInQueue(
                        user.TelegramUserId,
                        TranslationHelper.Instance.Translate(user.LanguageCode, "#%JOB_AUTH_LOST_PLEASE_LOGIN_AND_UPDATE_IT%#"),
                        isSilent: false
                    );

                    account.LastStatus = TranslationHelper.Instance.Translate(user.LanguageCode, "#%JOB_LAST_STATUS_UNAUTHORIZED%#");
                    accountRepository.Update(account);
                }

                isAuthGood = false;
            }
            else
            {
                Log.Information($"Daily Check Job, Auth is good for an account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, " +
                                $"BlumUsername: {account.BlumUsername}.");

                isAuthGood = true;

                account = accountRepository.GetById(accountId);
                if (account == null)
                {
                    Log.Error("Daily Check Job, the Account is NULL from DB after reauth.");
                    return;
                }

                // Doing Daily Reward Job
                (dailyClaimResponse, bool sameDay) = gameApiClient.GetDailyReward(account);
                if (dailyClaimResponse != ApiResponse.Success)
                {
                    Log.Error($"Daily Check Job, can't take daily reward for some reason for an account with Id: {account.Id}, " +
                              $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}. Is same day: {sameDay}.");
                }
                else
                {
                    Log.Information($"Daily Check Job, took daily reward for an account with Id: {account.Id}, " +
                                    $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");

                    dailyRewardRepository.Add(new DailyReward
                    {
                        AccountId = account.Id,
                        CreatedAt = DateTime.Now,
                    });
                }

                // Updating user info
                (getUserInfoResult, var gotBalance, int tickets) = gameApiClient.GetUserInfo(account);
                if (getUserInfoResult == ApiResponse.Success)
                {
                    account.Balance = gotBalance;
                    account.Tickets = tickets;
                    accountRepository.Update(account);

                    Log.Information($"Daily Check Job, balance is {gotBalance}, ticket's count is {tickets} " +
                                    $"for an account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, " +
                                    $"BlumUsername: {account.BlumUsername}.");
                }
                else
                {
                    Log.Error($"Daily Check Job, error while getting user info for an account with Id: {account.Id}, " +
                              $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");
                }

                // Doing claiming a farming stuff
                (ApiResponse claimResponse, double balanceFarming, tickets) = gameApiClient.ClaimFarming(account);
                if (claimResponse == ApiResponse.Success)
                {
                    Log.Information($"Daily Check Job, claimed farming successfully for an account with Id: {account.Id}, " +
                                    $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}, Tickets: {tickets}.");
                }
                else
                {
                    Log.Error($"Daily Check Job, error while claiming farming for an account with Id: {account.Id}, " +
                              $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");
                }

                // Doing starting farming stuff
                ApiResponse startFarmingResponse = gameApiClient.StartFarming(account);
                if (startFarmingResponse == ApiResponse.Success)
                {
                    Log.Information($"Daily Check Job, started farming successfully for an account with Id: {account.Id}, " +
                                    $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");
                }
                else
                {
                    Log.Error($"Daily Check Job, error while starting farming for an account with Id: {account.Id}, " +
                              $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");
                }

                // Starting and claiming all the tasks
                startAndClaimAllTasksIsGood = GameApiUtilsService.StartAndClaimAllTasks(account, earningRepository, gameApiClient);
                Log.Information($"Daily Check Job, ended working with tasks for an account with Id: {account.Id}, " +
                                $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");

                (ApiResponse responseFriendsBalance, bool canClaim, long canClaimAt, string referralToken, int referralsCount) 
                    = gameApiClient.FriendsBalance(account);
                if (responseFriendsBalance == ApiResponse.Success)
                {
                    account.ReferralsCount = referralsCount;
                    account.ReferralLink   = REFERRAL_LINK_PREFIX + referralToken;
                    accountRepository.Update(account);

                    Log.Information($"Daily Check Job, got friends balance (canClaim: {canClaim}, canClaimAt: {canClaimAt}, " +
                                    $"referralToken: {referralToken}, referralsCount: {referralsCount}) for an account Id: {account.Id}, " +
                                    $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");
                }
                else
                {
                    Log.Warning($"Daily Check Job, can't get friends balance for an account Id: {account.Id}, " +
                                $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");
                }
                if (responseFriendsBalance != ApiResponse.Success || (responseFriendsBalance == ApiResponse.Success && canClaim))
                {
                    // Claiming our possible reward for friends
                    friendsClaimResponse = gameApiClient.ClaimFriends(account);
                    if (friendsClaimResponse != ApiResponse.Success)
                    {
                        Log.Warning($"Daily Check Job, can't take friends reward for some reason for an account with Id: {account.Id}, " +
                                    $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");
                    }
                    else
                    {
                        Log.Information($"Daily Check Job, ended working with friends reward for an account Id: {account.Id}, " +
                                        $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");
                    }
                }

                // Playing games with all the tickets
                GameApiUtilsService.PlayGamesForAllTickets(account, accountRepository, earningRepository, gameApiClient);
                Log.Information($"Daily Check Job, ended playing for all tickets for an account Id: {account.Id}, " +
                                $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}, " +
                                $"Balance: {account.Balance}, Tickets: {account.Tickets}.");

                account = accountRepository.GetById(accountId);
                if (account == null)
                {
                    Log.Error("Daily Check Job, the Account is NULL from DB after playing for all tickets.");
                    return;
                }

                if (getUserInfoResult == ApiResponse.Success)
                {
                    Log.Information($"Daily Check Job, sheduling earning job for an account Id: {account.Id}, " +
                                    $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");
                    var startDate = DateTime.Now.AddMinutes(random.Next(EarningCheckJob.MIN_MINUTES_TO_WAIT, EarningCheckJob.MAX_MINUTES_TO_WAIT + 1));
                    await TaskScheduler.ScheduleEarningJob(taskScheduler, accountId, gotBalance, "ClaimFarming", startDate);
                }
            }

            if (isPlanned && task != null)
            {
                Log.Information($"Daily Check Job, planning future Jobs for an account Id: {account.Id}, " +
                                $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");

                DateTime nextRunTime;
                int randomSecondsNext = random.Next(task.MinScheduleSeconds, task.MaxScheduleSeconds);

                // Determine the next run time based on the result
                if (getUserInfoResult == ApiResponse.Success && 
                    account.Tickets == 0 && startAndClaimAllTasksIsGood && isAuthGood)
                {
                    Log.Information("Daily Check Job executed SUCCESSFULLY for an account with Id: " +
                                    $"{account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");

                    nextRunTime = DateTime.Now.AddSeconds(randomSecondsNext); // Запланировать задание снова через обычное кол-во времени

                    await TaskScheduler.ScheduleNewTask(taskScheduler, accountId, task, nextRunTime);

                    Log.Warning($"Daily Check Job is planned to be executed in ~{randomSecondsNext / 60} minutes - SUCCESSFUL - " +
                                $"for an account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, " +
                                $"BlumUsername: {account.BlumUsername}, Tickets Amount: {account.Tickets}.");
                }
                else
                {
                    nextRunTime = DateTime.Now.AddSeconds(randomSecondsNext / 10); // Запланировать задание снова через уменьшенное кол-во времени

                    await TaskScheduler.ScheduleNewTask(taskScheduler, accountId, task, nextRunTime);

                    Log.Warning($"Daily Check Job is planned to be executed in ~{randomSecondsNext / (10 * 60)} minutes because of not successful server's answer or tickets amount " +
                                $"not equals zero for an account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, " +
                                $"BlumUsername: {account.BlumUsername}, Tickets Amount: {account.Tickets}.");
                }

                // Обновление записи задачи в базе данных
                task.NextRunTime = nextRunTime;
                taskRepository.Update(task);
            }

            account.LastStatus = TranslationHelper.Instance.Translate(user.LanguageCode, "#%JOB_LAST_STATUS_OK%#");
            accountRepository.Update(account);

            Log.Information($"Daily Check Job is done for an account with Id: {account.Id}, " +
                            $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");
        }
    }
}
