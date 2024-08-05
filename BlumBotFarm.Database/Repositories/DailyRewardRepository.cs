using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace BlumBotFarm.Database.Repositories
{
    public class DailyRewardRepository : IRepository<DailyReward>
    {
        private readonly IMongoDatabase                _database;
        private readonly string                        _collectionName;
        private readonly IMongoCollection<DailyReward> _dailyRewards;

        public DailyRewardRepository(string connectionString, string databaseName, string collectionName)
        {
            var client      = new MongoClient(connectionString);
            _database       = client.GetDatabase(databaseName);
            _collectionName = collectionName;
            _dailyRewards   = _database.GetCollection<DailyReward>(_collectionName);
        }

        public IEnumerable<DailyReward> GetAll()
        {
            return _dailyRewards.Find(reward => true).ToList();
        }

        public IEnumerable<DailyReward> GetAllFit(Expression<Func<DailyReward, bool>> func)
        {
            return _dailyRewards.Find(func).ToList();
        }

        public DailyReward? GetById(int id)
        {
            return _dailyRewards.Find(reward => reward.Id == id).FirstOrDefault();
        }

        public int Add(DailyReward dailyReward)
        {
            if (dailyReward.Id == 0)
            {
                dailyReward.Id = AutoIncrement.GetNextSequence(_database, _collectionName + "_id");
            }
            else if (GetById(dailyReward.Id) != null)
            {
                throw new Exception("ID is incorrect!");
            }

            _dailyRewards.InsertOne(dailyReward);
            return dailyReward.Id;
        }

        public void Update(DailyReward dailyReward)
        {
            _dailyRewards.ReplaceOne(existingReward => existingReward.Id == dailyReward.Id, dailyReward);
        }

        public void Delete(int id)
        {
            _dailyRewards.DeleteOne(reward => reward.Id == id);
        }
    }
}