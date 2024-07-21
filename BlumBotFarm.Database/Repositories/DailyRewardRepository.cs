using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using Dapper;
using Microsoft.Data.Sqlite;

namespace BlumBotFarm.Database.Repositories
{
    public class DailyRewardRepository : IRepository<DailyReward>
    {
        private static readonly object _lock = new();
        private readonly string _connectionString;

        public DailyRewardRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<DailyReward> GetAll()
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    return db.Query<DailyReward>("SELECT * FROM DailyRewards").ToList();
                }
            }
        }

        public DailyReward? GetById(int id)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    return db.QuerySingleOrDefault<DailyReward>("SELECT * FROM DailyRewards WHERE Id = @Id", new { Id = id });
                }
            }
        }

        public int Add(DailyReward dailyReward)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = @"INSERT INTO DailyRewards (
                                    AccountId,
                                    CreatedAt)
                                VALUES (
                                    @AccountId,
                                    @CreatedAt);
                                SELECT last_insert_rowid();";
                    return db.ExecuteScalar<int>(sql, dailyReward);
                }
            }
        }

        public void Update(DailyReward dailyReward)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = @"UPDATE DailyRewards SET
                                    AccountId = @AccountId,
                                    CreatedAt = @CreatedAt
                                WHERE Id = @Id";
                    db.Execute(sql, dailyReward);
                }
            }
        }

        public void Delete(int id)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = "DELETE FROM DailyRewards WHERE Id = @Id";
                    db.Execute(sql, new { Id = id });
                }
            }
        }
    }
}