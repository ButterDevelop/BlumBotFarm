using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace BlumBotFarm.Database.Repositories
{
    public class EarningRepository : IRepository<Earning>
    {
        private readonly IMongoDatabase            _database;
        private readonly string                    _collectionName;
        private readonly IMongoCollection<Earning> _earnings;

        public EarningRepository(string connectionString, string databaseName, string collectionName)
        {
            var client      = new MongoClient(connectionString);
            _database       = client.GetDatabase(databaseName);
            _collectionName = collectionName;
            _earnings       = _database.GetCollection<Earning>(_collectionName);
        }

        public IEnumerable<Earning> GetAll()
        {
            return _earnings.Find(earning => true).ToList();
        }

        public IEnumerable<Earning> GetAllFit(Expression<Func<Earning, bool>> func)
        {
            return _earnings.Find(func).ToList();
        }

        public Earning? GetById(int id)
        {
            return _earnings.Find(earning => earning.Id == id).FirstOrDefault();
        }

        public int Add(Earning earning)
        {
            if (earning.Id == 0)
            {
                earning.Id = AutoIncrement.GetNextSequence(_database, _collectionName + "_id");
            }
            else if (GetById(earning.Id) != null)
            {
                throw new Exception("ID is incorrect!");
            }

            _earnings.InsertOne(earning);
            return earning.Id;
        }

        public void Update(Earning earning)
        {
            _earnings.ReplaceOne(existingEarning => existingEarning.Id == earning.Id, earning);
        }

        public void Delete(int id)
        {
            _earnings.DeleteOne(earning => earning.Id == id);
        }
    }
}