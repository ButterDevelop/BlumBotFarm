namespace BlumBotFarm.Database.Interfaces
{
    public interface IRepository<T>
    {
        IEnumerable<T> GetAll();
        T? GetById(int id);
        void Add(T obj);
        void Update(T obj);
        void Delete(int id);
    }
}
