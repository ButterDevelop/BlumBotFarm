using BlumBotFarm.Core;
using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Repositories;
using BlumBotFarm.GameClient;
using BlumBotFarm.Translation;
using Quartz;
using Serilog;

namespace BlumBotFarm.Scheduler.Jobs
{
    public class DailyCheckJob : IJob
    {
        private const string REFERRAL_LINK_PREFIX = "t.me/BlumCryptoBot/app?startapp=ref_";

        private static readonly Random StaticRandom = new();

        private readonly GameApiClient         gameApiClient;
        private readonly AccountRepository     accountRepository;
        private readonly TaskRepository        taskRepository;
        private readonly DailyRewardRepository dailyRewardRepository;
        private readonly UserRepository        userRepository;
        private readonly EarningRepository     earningRepository;
        private readonly ConfigModelRepository configModelRepository;
        private readonly TaskScheduler         taskScheduler;

        public DailyCheckJob()
        {
            gameApiClient          = new GameApiClient();

            var dbConnectionString = AppConfig.DatabaseSettings.MONGO_CONNECTION_STRING;
            var databaseName       = AppConfig.DatabaseSettings.MONGO_DATABASE_NAME;

            accountRepository      = new AccountRepository(dbConnectionString, databaseName, AppConfig.DatabaseSettings.MONGO_ACCOUNT_PATH);
            taskRepository         = new TaskRepository(dbConnectionString, databaseName, AppConfig.DatabaseSettings.MONGO_TASK_PATH);
            dailyRewardRepository  = new DailyRewardRepository(dbConnectionString, databaseName, AppConfig.DatabaseSettings.MONGO_DAILY_REWARDS_PATH);
            userRepository         = new UserRepository(dbConnectionString, databaseName, AppConfig.DatabaseSettings.MONGO_USER_PATH);
            earningRepository      = new EarningRepository(dbConnectionString, databaseName, AppConfig.DatabaseSettings.MONGO_EARNING_PATH);
            configModelRepository  = new ConfigModelRepository(dbConnectionString, databaseName, AppConfig.DatabaseSettings.MONGO_CONFIG_MODEL_PATH);
            taskScheduler          = new TaskScheduler();
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

            var user = userRepository.GetById(account.UserId);
            if (user == null)
            {
                Log.Error($"Exiting Daily Check Job because user was NULL for an account (Id: {account.Id}, CustomUsername: {account.CustomUsername}, " +
                            $"BlumUsername: {account.BlumUsername}).");
                return;
            }

            if (string.IsNullOrEmpty(account.ProviderToken))
            {
                account.LastStatus = "#%JOB_LAST_STATUS_UNAUTHORIZED%#";
                accountRepository.Update(account);

                Log.Warning($"Exiting Daily Check Job because of an account (Id: {account.Id}, CustomUsername: {account.CustomUsername}, " +
                            $"BlumUsername: {account.BlumUsername}) has no auth at all.");
                return;
            }

            if (account.IsTrial && DateTime.UtcNow >= account.TrialExpires)
            {
                MessageProcessor.MessageProcessor.Instance?.SendMessageToUserInQueue(
                        user.TelegramUserId,
                        TranslationHelper.Instance.Translate(user.LanguageCode, "#%JOB_TRIAL_EXPIRED%#"),
                        isSilent: false
                    );

                Log.Warning($"Exiting Daily Check Job because of an account (Id: {account.Id}, CustomUsername: {account.CustomUsername}, " +
                            $"BlumUsername: {account.BlumUsername}) has no active trial (expired).");
                return;
            }

            Log.Information($"Started Daily Check Job for an account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, " +
                            $"BlumUsername: {account.BlumUsername}");

            Random random = new();

            var configModel = configModelRepository.GetOrAddConfigModel();

            ApiResponse dailyClaimResponse = ApiResponse.Error, friendsClaimResponse = ApiResponse.Error, getUserInfoResult = ApiResponse.Error;
            bool startAndClaimAllTasksIsGood = true, isAuthGood = false, notChanceForPlaying = false;
            int  ticketsToRemainAfterPlaying = 0;

            // Auth check, first of all
            ApiResponse authCheckResult = ApiResponse.Error;
            if ((authCheckResult = GameApiUtilsService.AuthCheck(account, accountRepository, gameApiClient)) != ApiResponse.Success)
            {
                Log.Error($"Daily Check Job, GameApiUtilsService.AuthCheck: UNABLE TO REAUTH! Account with Id: {account.Id}, " +
                          $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");

                if (authCheckResult == ApiResponse.Unauthorized)
                {
                    MessageProcessor.MessageProcessor.Instance?.SendMessageToUserInQueue(
                        user.TelegramUserId,
                        TranslationHelper.Instance.Translate(user.LanguageCode, "#%JOB_AUTH_LOST_PLEASE_LOGIN_AND_UPDATE_IT%#"),
                        isSilent: false
                    );

                    account.LastStatus = "#%JOB_LAST_STATUS_UNAUTHORIZED%#";
                }
                else
                {
                    account.LastStatus = "#%JOB_LAST_STATUS_PROXY_PROBLEM%#";
                }
                accountRepository.Update(account);

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

                // Update account status to "doing work"
                account.LastStatus = "#%JOB_LAST_STATUS_WORKING_NOW%#";

                // Update account's eligibility for dogs drop
                var (response, eligibleForDogsDrop) = gameApiClient.EligibleForDogsDrop(account);
                if (response == ApiResponse.Success && account.IsEligibleForDogsDrop != eligibleForDogsDrop)
                {
                    account.IsEligibleForDogsDrop = eligibleForDogsDrop;
                }

                accountRepository.Update(account);

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

                float randomStaticNumber;
                if (configModel.EnableExecutingTasks)
                {
                    lock (StaticRandom)
                    {
                        randomStaticNumber = StaticRandom.NextSingle();
                    }
                    if (randomStaticNumber < configModel.ChanceForPlayingTicketsAndPlayingTasks)
                    {
                        // Starting and claiming all the tasks
                        startAndClaimAllTasksIsGood = GameApiUtilsService.StartAndClaimAllTasks(account, earningRepository, gameApiClient);
                        Log.Information($"Daily Check Job, ended working with tasks for an account with Id: {account.Id}, " +
                                        $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");
                    }
                    else
                    {
                        Log.Information($"Daily Check Job, we have been told by chances NOT to play for all tickets for an account Id: {account.Id}, " +
                                        $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}, " +
                                        $"Balance: {account.Balance}, Tickets: {account.Tickets}.");
                    }
                }

                (ApiResponse responseFriendsBalance, bool canClaim, string referralToken, int referralsCount) 
                    = gameApiClient.FriendsBalance(account);
                if (responseFriendsBalance == ApiResponse.Success)
                {
                    account.ReferralsCount = referralsCount;
                    account.ReferralLink   = REFERRAL_LINK_PREFIX + referralToken;
                    accountRepository.Update(account);

                    Log.Information($"Daily Check Job, got friends balance (canClaim: {canClaim}, " +
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

                if (configModel.EnablePlayingForTickets)
                {
                    lock (StaticRandom)
                    {
                        randomStaticNumber = StaticRandom.NextSingle();
                    }
                    if (StaticRandom.NextSingle() < configModel.ChanceForPlayingTicketsAndPlayingTasks)
                    {
                        int playHowMuchTickets      = random.Next(account.Tickets) + 1;
                        ticketsToRemainAfterPlaying = account.Tickets - playHowMuchTickets;

                        // Playing games with all the tickets
                        GameApiUtilsService.PlayGamesForAllTickets(account, accountRepository, gameApiClient, ticketsToRemainAfterPlaying);
                        Log.Information($"Daily Check Job, ended playing for all tickets for an account Id: {account.Id}, " +
                                        $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}, " +
                                        $"Balance: {account.Balance}, Tickets: {account.Tickets}.");
                    }
                    else
                    {
                        notChanceForPlaying = true;
                        Log.Information($"Daily Check Job, we have been told by chances NOT to play for all tickets for an account Id: {account.Id}, " +
                                        $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}, " +
                                        $"Balance: {account.Balance}, Tickets: {account.Tickets}.");
                    }
                }

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

                account.LastStatus = "#%JOB_LAST_STATUS_OK%#";
                accountRepository.Update(account);
            }

            if (isPlanned && task != null)
            {
                Log.Information($"Daily Check Job, planning future Jobs for an account Id: {account.Id}, " +
                                $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");

                DateTime nextRunTime;
                int randomSecondsNext = random.Next(task.MinScheduleSeconds, task.MaxScheduleSeconds);

                // If this is a trial than we will use it later (2 times in general)
                if (account.IsTrial) randomSecondsNext *= 2;

                // Determine the next run time based on the result
                if (getUserInfoResult == ApiResponse.Success && 
                    (notChanceForPlaying || account.Tickets == ticketsToRemainAfterPlaying) && startAndClaimAllTasksIsGood && isAuthGood)
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
                    nextRunTime = DateTime.Now.AddSeconds(randomSecondsNext / 5); // Запланировать задание снова через уменьшенное кол-во времени

                    await TaskScheduler.ScheduleNewTask(taskScheduler, accountId, task, nextRunTime);

                    Log.Warning($"Daily Check Job is planned to be executed in ~{randomSecondsNext / (10 * 60)} minutes because of not successful server's answer or tickets amount " +
                                $"not equals zero for an account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, " +
                                $"BlumUsername: {account.BlumUsername}, Tickets Amount: {account.Tickets}.");
                }

                // Обновление записи задачи в базе данных
                task.NextRunTime = nextRunTime;
                taskRepository.Update(task);
            }

            Log.Information($"Daily Check Job is done for an account with Id: {account.Id}, " +
                            $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");
        }
    }
}
