using BlumBotFarm.Core.Models;
using Dapper;
using System.Data;
using Task = BlumBotFarm.Core.Models.Task;

namespace BlumBotFarm.Database.Repositories
{
    public class MessageRepository
    {
        private static readonly object dbLock = new();

        private readonly IDbConnection _db;

        public MessageRepository(IDbConnection db)
        {
            _db = db;
        }

        public IEnumerable<Message> GetAll()
        {
            lock (dbLock)
            {
                return _db.Query<Message>("SELECT * FROM Messages").ToList();
            }
        }

        public Task? GetById(int id)
        {
            lock (dbLock)
            {
                return _db.QuerySingleOrDefault<Task>("SELECT * FROM Messages WHERE Id = @Id", new { Id = id });
            }
        }

        public void Add(Message message)
        {
            lock (dbLock)
            {
                var sql = "INSERT INTO Messages (ChatId, MessageText) VALUES (@ChatId, @MessageText)";
                _db.Execute(sql, message);
            }
        }

        public void Update(Message message)
        {
            lock (dbLock)
            {
                var sql = "UPDATE Messages SET ChatId = @ChatId, MessageText = @MessageText, CreatedAt = @CreatedAt WHERE Id = @Id";
                _db.Execute(sql, message);
            }
        }

        public void Delete(int id)
        {
            lock (dbLock)
            {
                var sql = "DELETE FROM Messages WHERE Id = @Id";
                _db.Execute(sql, new { Id = id });
            }
        }
    }
}
