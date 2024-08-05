using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace BlumBotFarm.Database.Repositories
{
    public class StarsPaymentRepository : IRepository<StarsPayment>
    {
        private readonly IMongoDatabase                 _database;
        private readonly string                         _collectionName;
        private readonly IMongoCollection<StarsPayment> _starsPayments;

        public StarsPaymentRepository(string connectionString, string databaseName, string collectionName)
        {
            var client      = new MongoClient(connectionString);
            _database       = client.GetDatabase(databaseName);
            _collectionName = collectionName;
            _starsPayments  = _database.GetCollection<StarsPayment>(_collectionName);
        }

        public IEnumerable<StarsPayment> GetAll()
        {
            return _starsPayments.Find(payment => true).ToList();
        }

        public IEnumerable<StarsPayment> GetAllFit(Expression<Func<StarsPayment, bool>> func)
        {
            return _starsPayments.Find(func).ToList();
        }

        public StarsPayment? GetById(int id)
        {
            return _starsPayments.Find(payment => payment.Id == id).FirstOrDefault();
        }

        public int Add(StarsPayment starsPayment)
        {
            if (starsPayment.Id == 0)
            {
                starsPayment.Id = AutoIncrement.GetNextSequence(_database, _collectionName + "_id");
            }
            else if (GetById(starsPayment.Id) != null)
            {
                throw new Exception("ID is incorrect!");
            }

            _starsPayments.InsertOne(starsPayment);
            return starsPayment.Id;
        }

        public void Update(StarsPayment starsPayment)
        {
            _starsPayments.ReplaceOne(existingPayment => existingPayment.Id == starsPayment.Id, starsPayment);
        }

        public void Delete(int id)
        {
            _starsPayments.DeleteOne(payment => payment.Id == id);
        }
    }
}
