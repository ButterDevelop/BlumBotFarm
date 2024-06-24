using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Repositories;
using Serilog;

namespace BlumBotFarm.GameClient
{
    public class GameApiUtilsService
    {
        private static readonly Random bombRandom = new();

        private const int MIN_GAME_POINTS               = 180,
                          MAX_GAME_POINTS               = 220,
                          BOMB_MINUS_AMOUNT             = -100,
                          BOMB_CLICK_CHANCE_PERCENT     = 1,
                          MIN_AMOUNT_OF_SECONDS_TO_WAIT = 35,
                          MAX_AMOUNT_OF_SECONDS_TO_WAIT = 50;

        public static bool AuthCheck(ref Account account, AccountRepository accountRepository, GameApiClient gameApiClient)
        {
            var result  = ApiResponse.Error;

            if ((result = gameApiClient.GetAboutMeInfo(account)) == ApiResponse.Unauthorized)
            {
                (ApiResponse refreshAuthResult, string newAccessToken, string newRefreshToken) = gameApiClient.RefreshAuth(account);

                if (refreshAuthResult != ApiResponse.Success)
                {
                    Log.Warning($"GameApiUtilsService AuthCheck not passed for account with Id: {account.Id}, Username: {account.Username}");
                    return false;
                }

                account.AccessToken  = newAccessToken;
                account.RefreshToken = newRefreshToken;
                accountRepository.Update(account);
            }

            return true;
        }

        public static void PlayGamesForAllTickets(ref Account account, AccountRepository accountRepository, GameApiClient gameApiClient)
        {
            Random random = new();

            int attempts = account.Tickets * 2;
            while (attempts-- > 0 && account.Tickets > 0)
            {
                (ApiResponse createGameResponse, string gameId) = gameApiClient.StartGame(account);

                if (createGameResponse == ApiResponse.Success)
                {
                    int secondsToSleep = random.Next(MIN_AMOUNT_OF_SECONDS_TO_WAIT, MAX_AMOUNT_OF_SECONDS_TO_WAIT);
                    Thread.Sleep(secondsToSleep * 1000);

                    int points = random.Next(MIN_GAME_POINTS, MAX_GAME_POINTS + 1);

                    // With a 1% we are clicking on the bomb. Lock because Random is not thread safe
                    lock (bombRandom)
                    {
                        if (bombRandom.Next(0, 100) < BOMB_CLICK_CHANCE_PERCENT)
                        {
                            points -= BOMB_MINUS_AMOUNT;
                            Log.Information($"GameApiUtilsService PlayGamesForAllTickets: Bomb worked with chance of {BOMB_CLICK_CHANCE_PERCENT}% for an " +
                                            $"account with Id: {account.Id}, Username: {account.Username}. " +
                                            $"Total points: {points}, bomb minus amount: {BOMB_MINUS_AMOUNT}");
                        }
                    }

                    var endGameResponse = gameApiClient.EndGame(account, gameId, points);
                    if (endGameResponse == ApiResponse.Success)
                    {
                        // Updating user info
                        (ApiResponse result, double balance, int tickets) = gameApiClient.GetUserInfo(account);
                        account.Balance = balance;
                        account.Tickets = tickets;
                        accountRepository.Update(account);

                        Thread.Sleep(1000 + random.Next(-100, 100));
                    }
                }
            }
        }

        public static bool StartAndClaimAllTasks(Account account, GameApiClient gameApiClient)
        {
            var random = new Random();
            int wholeCount = 0, errorsCount = 0;

            (ApiResponse response, var tasks) = gameApiClient.GetTasks(account);
            ++wholeCount;
            if (response != ApiResponse.Success) ++errorsCount;

            foreach (var task in tasks)
            {
                if (task.kind == "INITIAL" && task.status == "NOT_STARTED")
                {
                    response = gameApiClient.StartTask(account, task.id);
                    ++wholeCount;
                    if (response != ApiResponse.Success) ++errorsCount; else Thread.Sleep(1000 + random.Next(-100, 100));
                }
            }

            Thread.Sleep(3000 + random.Next(300, 600));

            (response, tasks) = gameApiClient.GetTasks(account);
            ++wholeCount;
            if (response != ApiResponse.Success) ++errorsCount;

            foreach (var task in tasks)
            {
                if (task.status == "READY_FOR_CLAIM")
                {
                    response = gameApiClient.ClaimTask(account, task.id);
                    ++wholeCount;
                    if (response != ApiResponse.Success) ++errorsCount; else Thread.Sleep(1000 + random.Next(-100, 100));
                }
            }

            // If the errors are less than 50%
            return ((double)errorsCount / wholeCount) < 0.5;
        }
    }
}
