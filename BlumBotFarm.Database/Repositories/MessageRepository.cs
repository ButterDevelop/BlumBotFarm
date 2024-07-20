using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using Dapper;
using System.Data;

namespace BlumBotFarm.Database.Repositories
{
    public class MessageRepository : IRepository<Message>
    {
        private readonly IDbConnection _db;

        public MessageRepository(IDbConnection db)
        {
            _db = db;
        }

        public IEnumerable<Message> GetAll()
        {
            lock (_db)
            {
                return _db.Query<Message>("SELECT * FROM Messages").ToList();
            }
        }

        public Message? GetById(int id)
        {
            lock (_db)
            {
                return _db.QuerySingleOrDefault<Message>("SELECT * FROM Messages WHERE Id = @Id", new { Id = id });
            }
        }

        public int Add(Message message)
        {
            lock (_db)
            {
                var sql = "INSERT INTO Messages (ChatId, MessageText, CreatedAt, IsSilent) VALUES (@ChatId, @MessageText, @CreatedAt, @IsSilent); " +
                          "SELECT last_insert_rowid();";
                return _db.ExecuteScalar<int>(sql, message);
            }
        }

        public void Update(Message message)
        {
            lock (_db)
            {
                var sql = "UPDATE Messages SET ChatId = @ChatId, MessageText = @MessageText, CreatedAt = @CreatedAt, IsSilent = @IsSilent WHERE Id = @Id";
                _db.Execute(sql, message);
            }
        }

        public void Delete(int id)
        {
            lock (_db)
            {
                var sql = "DELETE FROM Messages WHERE Id = @Id";
                _db.Execute(sql, new { Id = id });
            }
        }
    }
}
