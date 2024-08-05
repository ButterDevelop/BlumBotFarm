using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace BlumBotFarm.Database.Repositories
{
    public class MessageRepository : IRepository<Message>
    {
        private readonly IMongoDatabase            _database;
        private readonly string                    _collectionName;
        private readonly IMongoCollection<Message> _messages;

        public MessageRepository(string connectionString, string databaseName, string collectionName)
        {
            var client      = new MongoClient(connectionString);
            _database       = client.GetDatabase(databaseName);
            _collectionName = collectionName;
            _messages       = _database.GetCollection<Message>(_collectionName);
        }

        public IEnumerable<Message> GetAll()
        {
            return _messages.Find(message => true).ToList();
        }

        public IEnumerable<Message> GetAllFit(Expression<Func<Message, bool>> func)
        {
            return _messages.Find(func).ToList();
        }

        public Message? GetById(int id)
        {
            return _messages.Find(message => message.Id == id).FirstOrDefault();
        }

        public int Add(Message message)
        {
            if (message.Id == 0)
            {
                message.Id = AutoIncrement.GetNextSequence(_database, _collectionName + "_id");
            }
            else if (GetById(message.Id) != null)
            {
                throw new Exception("ID is incorrect!");
            }

            _messages.InsertOne(message);
            return message.Id;
        }

        public void Update(Message message)
        {
            _messages.ReplaceOne(existingMessage => existingMessage.Id == message.Id, message);
        }

        public void Delete(int id)
        {
            _messages.DeleteOne(message => message.Id == id);
        }
    }
}