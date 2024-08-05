using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace BlumBotFarm.Database.Repositories
{
    public class WalletPaymentRepository : IRepository<WalletPayment>
    {
        private readonly IMongoDatabase                  _database;
        private readonly string                          _collectionName;
        private readonly IMongoCollection<WalletPayment> _walletPayments;

        public WalletPaymentRepository(string connectionString, string databaseName, string collectionName)
        {
            var client      = new MongoClient(connectionString);
            _database       = client.GetDatabase(databaseName);
            _collectionName = collectionName;
            _walletPayments = _database.GetCollection<WalletPayment>(_collectionName);
        }

        public IEnumerable<WalletPayment> GetAll()
        {
            return _walletPayments.Find(payment => true).ToList();
        }

        public List<WalletPayment> GetAllFit(Expression<Func<WalletPayment, bool>> func)
        {
            return _walletPayments.Find(func).ToList();
        }

        public WalletPayment? GetById(int id)
        {
            return _walletPayments.Find(payment => payment.Id == id).FirstOrDefault();
        }

        public int Add(WalletPayment paymentTransaction)
        {
            if (paymentTransaction.Id == 0)
            {
                paymentTransaction.Id = AutoIncrement.GetNextSequence(_database, _collectionName + "_id");
            }
            else if (GetById(paymentTransaction.Id) != null)
            {
                throw new Exception("ID is incorrect!");
            }

            _walletPayments.InsertOne(paymentTransaction);
            return paymentTransaction.Id;
        }

        public void Update(WalletPayment paymentTransaction)
        {
            _walletPayments.ReplaceOne(existingPayment => existingPayment.Id == paymentTransaction.Id, paymentTransaction);
        }

        public void Delete(int id)
        {
            _walletPayments.DeleteOne(payment => payment.Id == id);
        }
    }
}