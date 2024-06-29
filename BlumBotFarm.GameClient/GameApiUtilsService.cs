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
                          BOMB_MINUS_ABS_AMOUNT         = 100,
                          BOMB_CLICK_CHANCE_PERCENT     = 1,
                          MIN_AMOUNT_OF_SECONDS_TO_WAIT = 35,
                          MAX_AMOUNT_OF_SECONDS_TO_WAIT = 50;

        public static bool AuthCheck(Account account, AccountRepository accountRepository, GameApiClient gameApiClient)
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

                Log.Information("GameApiUtilsService AuthCheck: successfully reauthenticated " +
                                $"for account with Id: {account.Id}, Username: {account.Username}");
            }

            Log.Information($"GameApiUtilsService AuthCheck: auth is actual for account with Id: {account.Id}, Username: {account.Username}");

            return true;
        }

        public static void PlayGamesForAllTickets(Account account, AccountRepository accountRepository, EarningRepository earningRepository, GameApiClient gameApiClient)
        {
            Random random = new();

            Log.Information("GameApiUtilsService PlayGamesForAllTickets: started playing for all tickets " +
                            $"for an account with Id: {account.Id}, Username: {account.Username}, tickets: {account.Tickets}");

            int attempts = account.Tickets * 2;
            while (attempts-- > 0 && account.Tickets > 0)
            {
                (ApiResponse createGameResponse, string gameId) = gameApiClient.StartGame(account);

                if (createGameResponse == ApiResponse.Success)
                {
                    Log.Information($"GameApiUtilsService PlayGamesForAllTickets: successfully started a game with id {gameId} " +
                                    $"for an account with Id: {account.Id}, Username: {account.Username}");

                    int secondsToSleep = random.Next(MIN_AMOUNT_OF_SECONDS_TO_WAIT, MAX_AMOUNT_OF_SECONDS_TO_WAIT);
                    Thread.Sleep(secondsToSleep * 1000);

                    int points = random.Next(MIN_GAME_POINTS, MAX_GAME_POINTS + 1);

                    // With a 1% we are clicking on the bomb. Lock because Random is not thread safe
                    lock (bombRandom)
                    {
                        if (bombRandom.Next(0, 100) < BOMB_CLICK_CHANCE_PERCENT)
                        {
                            points -= BOMB_MINUS_ABS_AMOUNT;
                            Log.Information($"GameApiUtilsService PlayGamesForAllTickets: Bomb worked with chance of {BOMB_CLICK_CHANCE_PERCENT}% for an " +
                                            $"account with Id: {account.Id}, Username: {account.Username}. " +
                                            $"Total points: {points}, bomb minus amount: {BOMB_MINUS_ABS_AMOUNT}");
                        }
                    }

                    var endGameResponse = gameApiClient.EndGame(account, gameId, points);
                    if (endGameResponse == ApiResponse.Success)
                    {
                        Log.Information($"GameApiUtilsService PlayGamesForAllTickets: successfully ended a game with id {gameId} and points {points} " +
                                        $"for an account with Id: {account.Id}, Username: {account.Username}");

                        // Updating user info
                        (ApiResponse result, double balance, int tickets) = gameApiClient.GetUserInfo(account);

                        if (result == ApiResponse.Success)
                        {
                            account.Balance = balance;
                            account.Tickets = tickets;
                            accountRepository.Update(account);

                            Log.Information("GameApiUtilsService PlayGamesForAllTickets: getting info after ending the game. " +
                                            $"Balance is {balance}, ticket's count is {tickets} " +
                                            $"for an account with Id: {account.Id}, Username: {account.Username}.");
                        }
                        else
                        {
                            Log.Error("GameApiUtilsService PlayGamesForAllTickets: error in getting user info " +
                                      $"for an account with Id: {account.Id}, Username: {account.Username}. Server answer: {result}, " +
                                      $"attempts: {attempts}, tickets: {account.Tickets}");
                        }

                        Thread.Sleep(1000 + random.Next(-100, 100));
                    }
                    else
                    {
                        Log.Error($"GameApiUtilsService PlayGamesForAllTickets: error in ending a game with id {gameId}, points {points} " +
                                  $"for an account with Id: {account.Id}, Username: {account.Username}. Server answer: {endGameResponse}, " +
                                  $"attempts: {attempts}, tickets: {account.Tickets}");
                    }
                }
                else
                {
                    Log.Error($"GameApiUtilsService PlayGamesForAllTickets: error in starting a game with id {gameId} " +
                              $"for an account with Id: {account.Id}, Username: {account.Username}. Server answer: {createGameResponse}, " +
                              $"attempts: {attempts}, tickets: {account.Tickets}");
                }
            }
        }

        public static bool StartAndClaimAllTasks(Account account, EarningRepository earningRepository, GameApiClient gameApiClient)
        {
            var random = new Random();
            int wholeCount = 0, errorsCount = 0;

            (ApiResponse response, var tasks) = gameApiClient.GetTasks(account);
            ++wholeCount;
            if (response != ApiResponse.Success)
            {
                Log.Error("GameApiUtilsService StartAndClaimAllTasks, error while trying to get tasks list " +
                          $"for an account Id: {account.Id}, Username: {account.Username}.");
                ++errorsCount;
            }
            else
            {
                Log.Information($"GameApiUtilsService StartAndClaimAllTasks, got tasks list " +
                                $"for an account Id: {account.Id}, Username: {account.Username}.");
            }

            foreach (var task in tasks)
            {
                if (task.kind == "INITIAL" && task.status == "NOT_STARTED")
                {
                    response = gameApiClient.StartTask(account, task.id);
                    ++wholeCount;
                    if (response != ApiResponse.Success)
                    {
                        Log.Error($"GameApiUtilsService StartAndClaimAllTasks, error while starting task ({task.id}, {task.kind}, {task.status}) " +
                                  $"for an account Id: {account.Id}, Username: {account.Username}.");
                        ++errorsCount;
                    }
                    else
                    {
                        Log.Information($"GameApiUtilsService StartAndClaimAllTasks, started task ({task.id}, {task.kind}, {task.status}) " +
                                        $"for an account Id: {account.Id}, Username: {account.Username}.");
                        Thread.Sleep(1000 + random.Next(-100, 100));
                    }
                }
            }

            Thread.Sleep(3000 + random.Next(300, 600));

            (response, tasks) = gameApiClient.GetTasks(account);
            ++wholeCount;
            if (response != ApiResponse.Success)
            {
                Log.Error($"GameApiUtilsService StartAndClaimAllTasks, error while trying to get tasks list once again " +
                          $"for an account Id: {account.Id}, Username: {account.Username}.");
                ++errorsCount;
            }
            else
            {
                Log.Information($"GameApiUtilsService StartAndClaimAllTasks, got tasks list once again " +
                                $"for an account Id: {account.Id}, Username: {account.Username}.");
            }

            foreach (var task in tasks)
            {
                if (task.status == "READY_FOR_CLAIM")
                {
                    (response, double reward) = gameApiClient.ClaimTask(account, task.id);
                    ++wholeCount;
                    if (response != ApiResponse.Success)
                    {
                        Log.Error($"GameApiUtilsService StartAndClaimAllTasks, error while claiming task ({task.id}, {task.kind}, {task.status}) for an account Id: {account.Id}, Username: {account.Username}.");
                        ++errorsCount;
                    }
                    else
                    {
                        Log.Information($"GameApiUtilsService StartAndClaimAllTasks, claimed task ({task.id}, {task.kind}, {task.status}) for an account Id: {account.Id}, Username: {account.Username}.");
                        Thread.Sleep(1000 + random.Next(-100, 100));
                    }
                }
            }

            // If the errors are less than 50%
            return ((double)errorsCount / wholeCount) < 0.5;
        }
    }
}
