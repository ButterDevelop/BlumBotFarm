using BlumBotFarm.Core.Models;
using BlumBotFarm.GameClient;

namespace BlumBotFarm.Tasks
{
    public class DailyRewardTask
    {
        private readonly GameApiClient gameApiClient;

        public DailyRewardTask()
        {
            gameApiClient = new GameApiClient();
        }

        public void Execute(Account account)
        {
            var response = gameApiClient.GetDailyReward(account);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Daily reward collected for {account.Username}");
            }
            else
            {
                Console.WriteLine($"Failed to collect daily reward for {account.Username}");
            }
        }
    }
}
