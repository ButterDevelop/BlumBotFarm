using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace BlumBotFarm.CacheUpdater.CacheServices
{
    public class UserCacheService : IUserCacheService
    {
        private readonly IMemoryCache   _cache;
        private readonly UserRepository _userRepository;

        public UserCacheService(IMemoryCache cache, UserRepository userRepository)
        {
            _cache          = cache;
            _userRepository = userRepository;
        }

        public bool TryGetFromCache(int userId, out User user)
        {
            if (_cache.TryGetValue($"User_{userId}", out User? gotUser))
            {
                if (gotUser != null)
                {
                    user = gotUser;
                    return true;
                }
            }

            gotUser = _userRepository.GetById(userId);
            if (gotUser != null && !gotUser.IsBanned)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                    SlidingExpiration               = TimeSpan.FromMinutes(2)
                };

                _cache.Set($"User_{userId}", gotUser, cacheEntryOptions);

                user = gotUser;
                return true;
            }

            user = new();
            return false;
        }

        public void SetInCache(User user)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                SlidingExpiration               = TimeSpan.FromMinutes(2)
            };

            _cache.Set($"User_{user.Id}", user, cacheEntryOptions);
        }
    }

}
