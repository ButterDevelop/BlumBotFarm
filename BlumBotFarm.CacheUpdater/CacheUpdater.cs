using BlumBotFarm.CacheUpdater.CacheServices;
using BlumBotFarm.Database.Repositories;

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
            var db                  = Database.Database.ConnectionString;
            _userRepository         = new UserRepository(db);
            _starsPaymentRepository = new StarsPaymentRepository(db);
            _userCacheService       = userCacheService;
            _cancellationToken      = cancellationToken;
        }

        public async Task StartAsync()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                Process();
                await Task.Delay(5000); // Ждем 5 секунд перед следующей проверкой
            }
        }

        private void Process()
        {
            var now       = DateTime.Now;
            var payments  = _starsPaymentRepository.GetAll().Where(p => p.IsCompleted && (now - p.CompletedDateTime).TotalSeconds < 5);
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
