using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using Dapper;
using System.Data;

namespace BlumBotFarm.Database.Repositories
{
    public class DailyRewardRepository : IRepository<DailyReward>
    {
        private readonly IDbConnection _db;

        public DailyRewardRepository(IDbConnection db)
        {
            _db = db;
        }

        public IEnumerable<DailyReward> GetAll()
        {
            lock (_db)
            {
                return _db.Query<DailyReward>("SELECT * FROM DailyRewards").ToList();
            }
        }

        public DailyReward? GetById(int id)
        {
            lock (_db)
            {
                return _db.QuerySingleOrDefault<DailyReward>("SELECT * FROM DailyRewards WHERE Id = @Id", new { Id = id });
            }
        }

        public int Add(DailyReward dailyReward)
        {
            lock (_db)
            {
                var sql = @"INSERT INTO DailyRewards (
                                AccountId,
                                CreatedAt)
                            VALUES (
                                @AccountId,
                                @CreatedAt);
                            SELECT last_insert_rowid();";
                return _db.ExecuteScalar<int>(sql, dailyReward);
            }
        }

        public void Update(DailyReward dailyReward)
        {
            lock (_db)
            {
                var sql = @"UPDATE DailyRewards SET
                                AccountId = @AccountId,
                                CreatedAt = @CreatedAt
                            WHERE Id = @Id";
                _db.Execute(sql, dailyReward);
            }
        }

        public void Delete(int id)
        {
            lock (_db)
            {
                var sql = "DELETE FROM DailyRewards WHERE Id = @Id";
                _db.Execute(sql, new { Id = id });
            }
        }
    }
}
