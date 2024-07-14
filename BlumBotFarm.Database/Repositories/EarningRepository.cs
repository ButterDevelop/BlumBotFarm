using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using Dapper;
using System.Data;

namespace BlumBotFarm.Database.Repositories
{
    public class EarningRepository : IRepository<Earning>
    {
        private static readonly object dbLock = new();

        private readonly IDbConnection _db;

        public EarningRepository(IDbConnection db)
        {
            _db = db;
        }

        public IEnumerable<Earning> GetAll()
        {
            lock (dbLock)
            {
                return _db.Query<Earning>("SELECT * FROM Earnings").ToList();
            }
        }

        public Earning? GetById(int id)
        {
            lock (dbLock)
            {
                return _db.QuerySingleOrDefault<Earning>("SELECT * FROM Earnings WHERE Id = @Id", new { Id = id });
            }
        }

        public int Add(Earning earning)
        {
            lock (dbLock)
            {
                var sql = "INSERT INTO Earnings (AccountId, Total, Created, Action) VALUES (@AccountId, @Total, @Created, @Action); " +
                          "SELECT last_insert_rowid();";
                return _db.ExecuteScalar<int>(sql, earning);
            }
        }

        public void Update(Earning earning)
        {
            lock (dbLock)
            {
                var sql = "UPDATE Earnings SET AccountId = @AccountId, Total = @Total, Created = @Created, Action = @Action WHERE Id = @Id";
                _db.Execute(sql, earning);
            }
        }

        public void Delete(int id)
        {
            lock (dbLock)
            {
                var sql = "DELETE FROM Earnings WHERE Id = @Id";
                _db.Execute(sql, new { Id = id });
            }
        }
    }
}
