using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using Dapper;
using Microsoft.Data.Sqlite;

namespace BlumBotFarm.Database.Repositories
{
    public class ReferralRepository : IRepository<Referral>
    {
        private static readonly object _lock = new();
        private readonly string _connectionString;

        public ReferralRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<Referral> GetAll()
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    return db.Query<Referral>("SELECT * FROM Referrals").ToList();
                }
            }
        }

        public Referral? GetById(int id)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    return db.QuerySingleOrDefault<Referral>("SELECT * FROM Referrals WHERE Id = @Id", new { Id = id });
                }
            }
        }

        public int Add(Referral referral)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = "INSERT INTO Referrals (HostUserId, DependentUserId) VALUES (@HostUserId, @DependentUserId); " +
                              "SELECT last_insert_rowid();";
                    return db.ExecuteScalar<int>(sql, referral);
                }
            }
        }

        public void Update(Referral referral)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = "UPDATE Referrals SET HostUserId = @HostUserId, DependentUserId = @DependentUserId WHERE Id = @Id";
                    db.Execute(sql, referral);
                }
            }
        }

        public void Delete(int id)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = "DELETE FROM Referrals WHERE Id = @Id";
                    db.Execute(sql, new { Id = id });
                }
            }
        }
    }
}