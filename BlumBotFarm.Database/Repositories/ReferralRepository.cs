using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using Dapper;
using System.Data;

namespace BlumBotFarm.Database.Repositories
{
    public class ReferralRepository : IRepository<Referral>
    {
        private readonly IDbConnection _db;

        public ReferralRepository(IDbConnection db)
        {
            _db = db;
        }

        public IEnumerable<Referral> GetAll()
        {
            lock (_db)
            {
                return _db.Query<Referral>("SELECT * FROM Referrals").ToList();
            }
        }

        public Referral? GetById(int id)
        {
            lock (_db)
            {
                return _db.QuerySingleOrDefault<Referral>("SELECT * FROM Referrals WHERE Id = @Id", new { Id = id });
            }
        }

        public int Add(Referral referral)
        {
            lock (_db)
            {
                var sql = "INSERT INTO Referrals (HostUserId, DependentUserId) VALUES (@HostUserId, @DependentUserId); " +
                          "SELECT last_insert_rowid();";
                return _db.ExecuteScalar<int>(sql, referral);
            }
        }

        public void Update(Referral referral)
        {
            lock (_db)
            {
                var sql = "UPDATE Referrals SET HostUserId = @HostUserId, DependentUserId = @DependentUserId WHERE Id = @Id";
                _db.Execute(sql, referral);
            }
        }

        public void Delete(int id)
        {
            lock (_db)
            {
                var sql = "DELETE FROM Referrals WHERE Id = @Id";
                _db.Execute(sql, new { Id = id });
            }
        }
    }
}
