using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace BlumBotFarm.Database.Repositories
{
    public class UserRepository : IRepository<User>
    {
        private readonly IMongoDatabase         _database;
        private readonly string                 _collectionName;
        private readonly IMongoCollection<User> _users;

        public UserRepository(string connectionString, string databaseName, string collectionName)
        {
            var client      = new MongoClient(connectionString);
            _database       = client.GetDatabase(databaseName);
            _collectionName = collectionName;
            _users          = _database.GetCollection<User>(_collectionName);
        }

        public IEnumerable<User> GetAll()
        {
            return _users.Find(user => true).ToList();
        }

        public IEnumerable<User> GetAllFit(Expression<Func<User, bool>> func)
        {
            return _users.Find(func).ToList();
        }

        public User? GetById(int id)
        {
            return _users.Find(user => user.Id == id).FirstOrDefault();
        }

        public int Add(User user)
        {
            if (user.Id == 0)
            {
                user.Id = AutoIncrement.GetNextSequence(_database, _collectionName + "_id");
            }
            else if (GetById(user.Id) != null)
            {
                throw new Exception("ID is incorrect!");
            }

            _users.InsertOne(user);
            return user.Id;
        }

        public void Update(User user)
        {
            _users.ReplaceOne(existingUser => existingUser.Id == user.Id, user);
        }

        public void Delete(int id)
        {
            _users.DeleteOne(user => user.Id == id);
        }
    }
}