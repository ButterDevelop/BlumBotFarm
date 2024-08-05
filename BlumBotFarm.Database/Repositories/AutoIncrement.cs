using MongoDB.Bson;
using MongoDB.Driver;

namespace BlumBotFarm.Database.Repositories
{
    public class AutoIncrement
    {
        public static int GetNextSequence(IMongoDatabase database, string name)
        {
            var collection = database.GetCollection<BsonDocument>("counters");
            var filter     = Builders<BsonDocument>.Filter.Eq("_id", name);
            var update     = Builders<BsonDocument>.Update.Inc("seq", 1);
            var options = new FindOneAndUpdateOptions<BsonDocument, BsonDocument>()
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert       = true
            };
            var result = collection.FindOneAndUpdate(filter, update, options);
            return result["seq"].AsInt32;
        }
    }
}
