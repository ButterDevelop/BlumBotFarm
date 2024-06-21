using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Repositories;

namespace BlumBotFarm.GameClient
{
    public class GameApiUtilsService
    {
        private const int MIN_GAME_POINTS = 200,
                          MAX_GAME_POINTS = 300,
                          MIN_AMOUNT_OF_SECONDS_TO_WAIT = 65,
                          MAX_AMOUNT_OF_SECONDS_TO_WAIT = 80;

        public static bool AuthCheck(ref Account account, AccountRepository accountRepository, GameApiClient gameApiClient)
        {
            var result  = ApiResponse.Error;

            if ((result = gameApiClient.GetAboutMeInfo(account)) == ApiResponse.Unauthorized)
            {
                (ApiResponse refreshAuthResult, string newAccessToken, string newRefreshToken) = gameApiClient.RefreshAuth(account);

                if (refreshAuthResult != ApiResponse.Success) return false;

                account.AccessToken = newAccessToken;
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

                    var endGameResponse = gameApiClient.EndGame(account, gameId, points);
                    if (endGameResponse == ApiResponse.Success)
                    {
                        // Updating user info
                        (ApiResponse result, double balance, int tickets) = gameApiClient.GetUserInfo(account);
                        account.Balance = balance;
                        account.Tickets = tickets;
                        accountRepository.Update(account);
                    }
                }
            }
        }

        public static void StartAndClaimAllTasks(Account account, GameApiClient gameApiClient)
        {
            (ApiResponse response, var tasks) = gameApiClient.GetTasks(account);

            foreach (var task in tasks)
            {
                if (task.kind == "INITIAL" && task.status == "NOT_STARTED")
                {
                    gameApiClient.StartTask(account, task.id);
                }
            }

            Thread.Sleep(3000);

            (response, tasks) = gameApiClient.GetTasks(account);

            foreach (var task in tasks)
            {
                if (task.status == "READY_FOR_CLAIM")
                {
                    gameApiClient.ClaimTask(account, task.id);
                }
            }
        }
    }
}
