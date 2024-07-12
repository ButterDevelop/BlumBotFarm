using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using Dapper;
using System.Data;

namespace BlumBotFarm.Database.Repositories
{
    public class MessageRepository : IRepository<Message>
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

        public Message? GetById(int id)
        {
            lock (dbLock)
            {
                return _db.QuerySingleOrDefault<Message>("SELECT * FROM Messages WHERE Id = @Id", new { Id = id });
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
