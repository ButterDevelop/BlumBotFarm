namespace AutoBlumFarmServer.CacheServices
{
    public interface ICacheService<T>
    {
        bool TryGetFromCache(int userId, out T user);
        void SetInCache(T user);
    }
}
