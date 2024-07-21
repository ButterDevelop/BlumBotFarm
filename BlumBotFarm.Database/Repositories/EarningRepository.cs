using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using Dapper;
using Microsoft.Data.Sqlite;

namespace BlumBotFarm.Database.Repositories
{
    public class EarningRepository : IRepository<Earning>
    {
        private static readonly object _lock = new();
        private readonly string _connectionString;

        public EarningRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<Earning> GetAll()
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    return db.Query<Earning>("SELECT * FROM Earnings").ToList();
                }
            }
        }

        public Earning? GetById(int id)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    return db.QuerySingleOrDefault<Earning>("SELECT * FROM Earnings WHERE Id = @Id", new { Id = id });
                }
            }
        }

        public int Add(Earning earning)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = "INSERT INTO Earnings (AccountId, Total, Created, Action) VALUES (@AccountId, @Total, @Created, @Action); " +
                              "SELECT last_insert_rowid();";
                    return db.ExecuteScalar<int>(sql, earning);
                }
            }
        }

        public void Update(Earning earning)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = "UPDATE Earnings SET AccountId = @AccountId, Total = @Total, Created = @Created, Action = @Action WHERE Id = @Id";
                    db.Execute(sql, earning);
                }
            }
        }

        public void Delete(int id)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = "DELETE FROM Earnings WHERE Id = @Id";
                    db.Execute(sql, new { Id = id });
                }
            }
        }
    }
}