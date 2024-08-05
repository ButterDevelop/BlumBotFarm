using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace BlumBotFarm.Database.Repositories
{
    public class ReferralRepository : IRepository<Referral>
    {
        private readonly IMongoDatabase             _database;
        private readonly string                     _collectionName;
        private readonly IMongoCollection<Referral> _referrals;

        public ReferralRepository(string connectionString, string databaseName, string collectionName)
        {
            var client      = new MongoClient(connectionString);
            _database       = client.GetDatabase(databaseName);
            _collectionName = collectionName;
            _referrals      = _database.GetCollection<Referral>(_collectionName);
        }

        public IEnumerable<Referral> GetAll()
        {
            return _referrals.Find(referral => true).ToList();
        }

        public IEnumerable<Referral> GetAllFit(Expression<Func<Referral, bool>> func)
        {
            return _referrals.Find(func).ToList();
        }

        public Referral? GetById(int id)
        {
            return _referrals.Find(referral => referral.Id == id).FirstOrDefault();
        }

        public int Add(Referral referral)
        {
            if (referral.Id == 0)
            {
                referral.Id = AutoIncrement.GetNextSequence(_database, _collectionName + "_id");
            }
            else if (GetById(referral.Id) != null)
            {
                throw new Exception("ID is incorrect!");
            }

            _referrals.InsertOne(referral);
            return referral.Id;
        }

        public void Update(Referral referral)
        {
            _referrals.ReplaceOne(existingReferral => existingReferral.Id == referral.Id, referral);
        }

        public void Delete(int id)
        {
            _referrals.DeleteOne(referral => referral.Id == id);
        }
    }
}