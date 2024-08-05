using BlumBotFarm.Database.Interfaces;
using MongoDB.Driver;
using System.Linq.Expressions;
using Task = BlumBotFarm.Core.Models.Task;

namespace BlumBotFarm.Database.Repositories
{
    public class TaskRepository : IRepository<Task>
    {
        private readonly IMongoDatabase         _database;
        private readonly string                 _collectionName;
        private readonly IMongoCollection<Task> _tasks;

        public TaskRepository(string connectionString, string databaseName, string collectionName)
        {
            var client      = new MongoClient(connectionString);
            _database       = client.GetDatabase(databaseName);
            _collectionName = collectionName;
            _tasks          = _database.GetCollection<Task>(_collectionName);
        }

        public IEnumerable<Task> GetAll()
        {
            return _tasks.Find(task => true).ToList();
        }

        public IEnumerable<Task> GetAllFit(Expression<Func<Task, bool>> func)
        {
            return _tasks.Find(func).ToList();
        }

        public Task? GetById(int id)
        {
            return _tasks.Find(task => task.Id == id).FirstOrDefault();
        }

        public int Add(Task task)
        {
            if (task.Id == 0)
            {
                task.Id = AutoIncrement.GetNextSequence(_database, _collectionName + "_id");
            }
            else if (GetById(task.Id) != null)
            {
                throw new Exception("ID is incorrect!");
            }

            _tasks.InsertOne(task);
            return task.Id;
        }

        public void Update(Task task)
        {
            _tasks.ReplaceOne(existingTask => existingTask.Id == task.Id, task);
        }

        public void Delete(int id)
        {
            _tasks.DeleteOne(task => task.Id == id);
        }
    }
}