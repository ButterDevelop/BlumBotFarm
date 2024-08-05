using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace BlumBotFarm.Database.Repositories
{
    public class AccountRepository : IRepository<Account>
    {
        private readonly IMongoDatabase            _database;
        private readonly string                    _collectionName;
        private readonly IMongoCollection<Account> _accounts;

        public AccountRepository(string connectionString, string databaseName, string collectionName)
        {
            var client      = new MongoClient(connectionString);
            _database       = client.GetDatabase(databaseName);
            _collectionName = collectionName;
            _accounts       = _database.GetCollection<Account>(_collectionName);
        }

        public IEnumerable<Account> GetAll()
        {
            return _accounts.Find(account => true).ToList();
        }

        public IEnumerable<Account> GetAllFit(Expression<Func<Account, bool>> func)
        {
            return _accounts.Find(func).ToList();
        }

        public Account? GetById(int id)
        {
            return _accounts.Find(account => account.Id == id).FirstOrDefault();
        }

        public int Add(Account account)
        {
            if (account.Id == 0)
            {
                account.Id = AutoIncrement.GetNextSequence(_database, _collectionName + "_id");
            }
            else if (GetById(account.Id) != null)
            {
                throw new Exception("ID is incorrect!");
            }

            _accounts.InsertOne(account);
            return account.Id;
        }

        public void Update(Account account)
        {
            _accounts.ReplaceOne(existingAccount => existingAccount.Id == account.Id, account);
        }

        public void Delete(int id)
        {
            _accounts.DeleteOne(account => account.Id == id);
        }
    }
}