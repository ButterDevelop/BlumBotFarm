using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Repositories;
using Serilog;

namespace BlumBotFarm.GameClient
{
    public class GameApiUtilsService
    {
        private const int FREEZE_HOW_MUCH_SECONDS = 5;
        private const string CLOVER_NAME = "CLOVER", BOMB_NAME = "BOMB", FREEZE_NAME = "FREEZE";
        private static readonly Random bombRandom = new();
        private static readonly string[] EXCLUDE_ASSETS_NAMES = [BOMB_NAME, FREEZE_NAME];

        private const int MAX_PIECES_PER_GAME                        = 300,
                          BOMB_MINUS_ABS_AMOUNT                      = 100,
                          BOMB_CLICK_CHANCE_PERCENT                  = 2,
                          MIN_AMOUNT_OF_SECONDS_TO_WAIT_IN_DROP_GAME = 35,
                          MAX_AMOUNT_OF_SECONDS_TO_WAIT_IN_DROP_GAME = 75,
                          MIN_GAME_POINTS_PERCENT                    = 60,
                          MAX_GAME_POINTS_PERCENT                    = 70,
                          MIN_GAME_POINTS_TRIAL_PERCENT              = 4,
                          MAX_GAME_POINTS_TRIAL_PERCENT              = 11;

        public static ApiResponse AuthCheck(Account account, AccountRepository accountRepository, GameApiClient gameApiClient)
        {
            (var result, string blumUsername) = gameApiClient.GetAboutMeInfo(account);

            if (result == ApiResponse.Unauthorized)
            {
                (ApiResponse refreshAuthResult, string newAccessToken, string newRefreshToken) = gameApiClient.RefreshAuth(account);

                if (refreshAuthResult != ApiResponse.Success)
                {
                    if (string.IsNullOrEmpty(account.ProviderToken))
                    {
                        Log.Warning("GameApiUtilsService AuthCheck: provider token IS EMPTY " +
                                    $"for account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}. " +
                                    $"Return Unauthorized.");
                        return ApiResponse.Unauthorized;
                    }

                    (ApiResponse getAuthByProviderResult, newAccessToken, newRefreshToken) = gameApiClient.GetAuthByProvider(account);
                    if (getAuthByProviderResult != ApiResponse.Success)
                    {
                        Log.Warning($"GameApiUtilsService AuthCheck not passed for account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}");
                        return getAuthByProviderResult;
                    }
                }

                account.AccessToken  = newAccessToken;
                account.RefreshToken = newRefreshToken;
                accountRepository.Update(account);

                result = ApiResponse.Success;

                Log.Information("GameApiUtilsService AuthCheck: successfully reauthenticated " +
                                $"for account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}");
            }
            else
            if (result == ApiResponse.Success)
            {
                if (string.IsNullOrEmpty(account.BlumUsername) && !string.IsNullOrEmpty(blumUsername))
                {
                    account.BlumUsername = blumUsername;
                    accountRepository.Update(account);

                    Log.Information($"GameApiUtilsService AuthCheck: updated Blum Username for account with Id: {account.Id}, " +
                                    $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}");
                }

                Log.Information($"GameApiUtilsService AuthCheck: auth is actual for account with Id: {account.Id}, " +
                                $"CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}");
            }
            else
            {
                Log.Information("GameApiUtilsService AuthCheck: can't get auth (probably smth is wrong with proxy) " +
                                $"for account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}");
            }

            return result;
        }

        public static void PlayGamesForAllTickets(Account account, AccountRepository accountRepository, GameApiClient gameApiClient, 
                                                  int ticketsToRemainAfterPlaying)
        {
            Random random = new();

            Log.Information("GameApiUtilsService PlayGamesForAllTickets: started playing for all tickets " +
                            $"for an account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}, tickets: {account.Tickets}");

            int attempts = account.IsTrial ? (account.Tickets <= 20 ? account.Tickets : 20) : (account.Tickets * 2);
            while (attempts-- > 0 && account.Tickets > Math.Max(0, ticketsToRemainAfterPlaying))
            {
                (ApiResponse createGameResponse, string gameId, Dictionary<string, (int perClick, double probability)> resultPieces) 
                    = gameApiClient.StartGame(account);

                if (createGameResponse == ApiResponse.Success)
                {
                    Log.Information($"GameApiUtilsService PlayGamesForAllTickets: successfully started a game with id {gameId} " +
                                    $"for an account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}");

                    int secondsToSleep = random.Next(MIN_AMOUNT_OF_SECONDS_TO_WAIT_IN_DROP_GAME, MAX_AMOUNT_OF_SECONDS_TO_WAIT_IN_DROP_GAME + 1);
                    Thread.Sleep(secondsToSleep * 1000);

                    double percentForPoints = (account.IsTrial ? random.Next(MIN_GAME_POINTS_TRIAL_PERCENT, MAX_GAME_POINTS_TRIAL_PERCENT + 1) :
                                                                 random.Next(MIN_GAME_POINTS_PERCENT, MAX_GAME_POINTS_PERCENT + 1)
                                              ) / 100.0;
                    int maxPiecesAmount = (int)(percentForPoints * MAX_PIECES_PER_GAME);

                    int wholeBpAmount = 0;
                    Dictionary<string, int> resultsAssets = [];
                    foreach (var piece in resultPieces)
                    {
                        string assetName = piece.Key;

                        if (!EXCLUDE_ASSETS_NAMES.Contains(assetName))
                        {
                            var (perClick, probability) = piece.Value;

                            int amount = (int)(probability * maxPiecesAmount);

                            wholeBpAmount += amount * perClick;

                            resultsAssets.Add(assetName, amount);
                        }
                    }

                    // Adding excluded assets separately with 0 value to work further
                    foreach (var excludedAsset in EXCLUDE_ASSETS_NAMES)
                    {
                        resultsAssets.Add(excludedAsset, 0);
                    }

                    // With a 1-2% we are clicking on the bomb. Lock because Random is not thread safe
                    lock (bombRandom)
                    {
                        if (bombRandom.Next(0, 100) < BOMB_CLICK_CHANCE_PERCENT && resultsAssets.ContainsKey(CLOVER_NAME))
                        {
                            resultsAssets[BOMB_NAME] += 1;
                            resultsAssets[CLOVER_NAME] -= Math.Min(BOMB_MINUS_ABS_AMOUNT, resultsAssets[CLOVER_NAME]);

                            Log.Information($"GameApiUtilsService PlayGamesForAllTickets: Bomb worked with chance of {BOMB_CLICK_CHANCE_PERCENT}% for an " +
                                            $"account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}. " +
                                            $"Total points: {resultsAssets[CLOVER_NAME]}, bomb minus amount: {BOMB_MINUS_ABS_AMOUNT}");
                        }
                    }

                    // Adding how much freeze we clicked
                    int freezeAmount = (secondsToSleep - MIN_AMOUNT_OF_SECONDS_TO_WAIT_IN_DROP_GAME) / FREEZE_HOW_MUCH_SECONDS;
                    resultsAssets[FREEZE_NAME] = freezeAmount;

                    // Joining full assets string for logs
                    string allAssetsString = string.Join(", ", resultsAssets.Select(r => r.Key + ": " + r.Value));

                    var endGameResponse = gameApiClient.EndGame(account, gameId, wholeBpAmount, resultsAssets);
                    if (endGameResponse == ApiResponse.Success)
                    {
                        Log.Information($"GameApiUtilsService PlayGamesForAllTickets: successfully ended a game with id {gameId} " +
                                        $"and points [{allAssetsString}], " +
                                        $"is eligibile for dogs drop: {account.IsEligibleForDogsDrop}, " +
                                        $"for an account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}");

                        // Updating user info
                        (ApiResponse result, double balance, int tickets) = gameApiClient.GetUserInfo(account);

                        if (result == ApiResponse.Success)
                        {
                            account.Balance = balance;
                            account.Tickets = tickets;
                            accountRepository.Update(account);

                            Log.Information("GameApiUtilsService PlayGamesForAllTickets: getting info after ending the game. " +
                                            $"Balance is {balance}, ticket's count is {tickets} " +
                                            $"for an account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}, Is Eligibile for Dogs Drop: {account.IsEligibleForDogsDrop}.");
                        }
                        else
                        {
                            Log.Error("GameApiUtilsService PlayGamesForAllTickets: error in getting user info " +
                                      $"for an account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}, " +
                                      $"Is Eligibile for Dogs Drop: {account.IsEligibleForDogsDrop}. " +
                                      $"Server answer: {result}, " +
                                      $"attempts: {attempts}, tickets: {account.Tickets}");
                        }

                        Thread.Sleep(2500 + random.Next(-2000, 2000));
                    }
                    else
                    {
                        Log.Error($"GameApiUtilsService PlayGamesForAllTickets: error in ending a game with id {gameId}, points [{allAssetsString}] " +
                                  $"for an account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}, " +
                                  $"Is Eligible for Dogs Drop: {account.IsEligibleForDogsDrop}. " +
                                  $"Server answer: {endGameResponse}, attempts: {attempts}, tickets: {account.Tickets}");
                    }
                }
                else
                {
                    Log.Error($"GameApiUtilsService PlayGamesForAllTickets: error in starting a game with id {gameId} " +
                              $"for an account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}. " +
                              $"Is Eligible for Dogs Drop: {account.IsEligibleForDogsDrop}, " +
                              $"Server answer: {createGameResponse}, attempts: {attempts}, tickets: {account.Tickets}");
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
                          $"for an account Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");
                ++errorsCount;
            }
            else
            {
                Log.Information($"GameApiUtilsService StartAndClaimAllTasks, got tasks list " +
                                $"for an account Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");
            }

            List<int> taskToClaimIndexes = [];

            for (int index = 0; index < tasks.Count; index++)
            {
                var (id, kind, status, validationType) = tasks[index];

                if ((kind == "INITIAL" || kind == "ROUTINE") && status == "NOT_STARTED" && validationType == "DEFAULT")
                {
                    ++wholeCount;
                    response = gameApiClient.StartTask(account, id);
                    if (response != ApiResponse.Success)
                    {
                        Log.Error($"GameApiUtilsService StartAndClaimAllTasks, error while starting task ({id}, {kind}, {status}) " +
                                  $"for an account Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");
                        ++errorsCount;
                    }
                    else
                    {
                        taskToClaimIndexes.Add(index);

                        Log.Information($"GameApiUtilsService StartAndClaimAllTasks, started task ({id}, {kind}, {status}) " +
                                        $"for an account Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");
                        Thread.Sleep(5000 + random.Next(1000, 2500));
                    }
                }
            }

            Thread.Sleep(10000 + random.Next(100, 1000));

            for (int index = 0; index < tasks.Count; index++)
            {
                var (id, kind, status, validationType) = tasks[index];

                if (status == "READY_FOR_CLAIM" || taskToClaimIndexes.Contains(index))
                {
                    (response, double reward) = gameApiClient.ClaimTask(account, id);
                    ++wholeCount;
                    if (response != ApiResponse.Success)
                    {
                        Log.Error($"GameApiUtilsService StartAndClaimAllTasks, error while claiming task ({id}, {kind}, {status}, {validationType}) for an account Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");
                        ++errorsCount;
                    }
                    else
                    {
                        Log.Information($"GameApiUtilsService StartAndClaimAllTasks, claimed task ({id}, {kind}, {status}, {validationType}) for an account Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}.");
                        Thread.Sleep(5000 + random.Next(-1000, 1500));
                    }
                }
            }

            // If the errors are less than 50%
            return ((double)errorsCount / wholeCount) < 0.5;
        }
    }
}
