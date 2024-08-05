using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace BlumBotFarm.Database.Repositories
{
    public class FeedbackMessageRepository : IRepository<FeedbackMessage>
    {
        private readonly IMongoDatabase                    _database;
        private readonly string                            _collectionName;
        private readonly IMongoCollection<FeedbackMessage> _feedbackMessages;

        public FeedbackMessageRepository(string connectionString, string databaseName, string collectionName)
        {
            var client        = new MongoClient(connectionString);
            _database         = client.GetDatabase(databaseName);
            _collectionName   = collectionName;
            _feedbackMessages = _database.GetCollection<FeedbackMessage>(_collectionName);
        }

        public IEnumerable<FeedbackMessage> GetAll()
        {
            return _feedbackMessages.Find(feedbackMessage => true).ToList();
        }

        public IEnumerable<FeedbackMessage> GetAllFit(Expression<Func<FeedbackMessage, bool>> func)
        {
            return _feedbackMessages.Find(func).ToList();
        }

        public FeedbackMessage? GetById(int id)
        {
            return _feedbackMessages.Find(feedbackMessage => feedbackMessage.Id == id).FirstOrDefault();
        }

        public int Add(FeedbackMessage feedbackMessage)
        {
            if (feedbackMessage.Id == 0)
            {
                feedbackMessage.Id = AutoIncrement.GetNextSequence(_database, _collectionName + "_id");
            }
            else if (GetById(feedbackMessage.Id) != null)
            {
                throw new Exception("ID is incorrect!");
            }

            _feedbackMessages.InsertOne(feedbackMessage);
            return feedbackMessage.Id;
        }

        public void Update(FeedbackMessage feedbackMessage)
        {
            _feedbackMessages.ReplaceOne(existingFeedbackMessage => existingFeedbackMessage.Id == feedbackMessage.Id, feedbackMessage);
        }

        public void Delete(int id)
        {
            _feedbackMessages.DeleteOne(feedbackMessage => feedbackMessage.Id == id);
        }
    }
}