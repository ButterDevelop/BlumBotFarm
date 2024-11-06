using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace BlumBotFarm.Database.Repositories
{
    public class ConfigModelRepository : IRepository<ConfigModel>
    {
        private readonly IMongoDatabase                _database;
        private readonly string                        _collectionName;
        private readonly IMongoCollection<ConfigModel> _configModels;

        public ConfigModelRepository(string connectionString, string databaseName, string collectionName)
        {
            var client      = new MongoClient(connectionString);
            _database       = client.GetDatabase(databaseName);
            _collectionName = collectionName;
            _configModels   = _database.GetCollection<ConfigModel>(_collectionName);
        }

        public IEnumerable<ConfigModel> GetAll()
        {
            return _configModels.Find(reward => true).ToList();
        }

        public IEnumerable<ConfigModel> GetAllFit(Expression<Func<ConfigModel, bool>> func)
        {
            return _configModels.Find(func).ToList();
        }

        public ConfigModel? GetById(int id)
        {
            return _configModels.Find(reward => reward.Id == id).FirstOrDefault();
        }

        public int Add(ConfigModel configModel)
        {
            if (configModel.Id == 0)
            {
                configModel.Id = AutoIncrement.GetNextSequence(_database, _collectionName + "_id");
            }
            else if (GetById(configModel.Id) != null)
            {
                throw new Exception("ID is incorrect!");
            }

            _configModels.InsertOne(configModel);
            return configModel.Id;
        }

        public void Update(ConfigModel configModel)
        {
            _configModels.ReplaceOne(existingReward => existingReward.Id == configModel.Id, configModel);
        }

        public void Delete(int id)
        {
            _configModels.DeleteOne(reward => reward.Id == id);
        }

        public ConfigModel GetOrAddConfigModel()
        {
            ConfigModel configModel;
            var allConfigModels = GetAll();
            if (!allConfigModels.Any())
            {
                configModel    = new();
                int id         = Add(configModel);
                configModel.Id = id;
            }
            else
            {
                configModel = allConfigModels.First();
            }
            return configModel;
        }
    }
}