using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using Dapper;
using Microsoft.Data.Sqlite;

namespace BlumBotFarm.Database.Repositories
{
    public class MessageRepository : IRepository<Message>
    {
        private static readonly object _lock = new();
        private readonly string _connectionString;

        public MessageRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<Message> GetAll()
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    return db.Query<Message>("SELECT * FROM Messages").ToList();
                }
            }
        }

        public Message? GetById(int id)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    return db.QuerySingleOrDefault<Message>("SELECT * FROM Messages WHERE Id = @Id", new { Id = id });
                }
            }
        }

        public int Add(Message message)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = "INSERT INTO Messages (ChatId, MessageText, CreatedAt, IsSilent) VALUES (@ChatId, @MessageText, @CreatedAt, @IsSilent); " +
                              "SELECT last_insert_rowid();";
                    return db.ExecuteScalar<int>(sql, message);
                }
            }
        }

        public void Update(Message message)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = "UPDATE Messages SET ChatId = @ChatId, MessageText = @MessageText, CreatedAt = @CreatedAt, IsSilent = @IsSilent WHERE Id = @Id";
                    db.Execute(sql, message);
                }
            }
        }

        public void Delete(int id)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = "DELETE FROM Messages WHERE Id = @Id";
                    db.Execute(sql, new { Id = id });
                }
            }
        }
    }
}