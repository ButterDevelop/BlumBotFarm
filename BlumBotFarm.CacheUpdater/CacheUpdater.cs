using BlumBotFarm.CacheUpdater.CacheServices;
using BlumBotFarm.Core;
using BlumBotFarm.Database.Repositories;
using Serilog;

namespace BlumBotFarm.CacheUpdater
{
    public class CacheUpdater
    {
        private readonly UserRepository         _userRepository;
        private readonly StarsPaymentRepository _starsPaymentRepository;
        private readonly IUserCacheService      _userCacheService;
        private readonly CancellationToken      _cancellationToken;

        public CacheUpdater(CancellationToken cancellationToken, IUserCacheService userCacheService)
        {
            var dbConnectionString  = AppConfig.DatabaseSettings.MONGO_CONNECTION_STRING;
            var databaseName        = AppConfig.DatabaseSettings.MONGO_DATABASE_NAME;
            _userRepository         = new UserRepository(dbConnectionString, databaseName, AppConfig.DatabaseSettings.MONGO_USER_PATH);
            _starsPaymentRepository = new StarsPaymentRepository(dbConnectionString, databaseName, AppConfig.DatabaseSettings.MONGO_STARS_PAYMENT_PATH);
            _userCacheService       = userCacheService;
            _cancellationToken      = cancellationToken;
        }

        public async Task StartAsync()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                Process();
                await Task.Delay(60000); // Ждем 60 секунд перед следующей проверкой
            }
        }

        private void Process()
        {
            var dateToCompare = DateTime.Now.AddSeconds(-65);
            var payments      = _starsPaymentRepository.GetAllFit(p => p.IsCompleted && p.CompletedDateTime >= dateToCompare);
            Random random = new();

            foreach (var payment in payments)
            {
                var user = _userRepository.GetById(payment.UserId);
                if (user == null) continue;

                _userCacheService.SetInCache(user);
            }
        }
    }
}
