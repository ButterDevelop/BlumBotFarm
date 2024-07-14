using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using Dapper;
using System.Data;
using System.Threading.Tasks;

namespace BlumBotFarm.Database.Repositories
{
    public class StarsPaymentRepository : IRepository<StarsPayment>
    {
        private static readonly object dbLock = new();

        private readonly IDbConnection _db;

        public StarsPaymentRepository(IDbConnection db)
        {
            _db = db;
        }

        public IEnumerable<StarsPayment> GetAll()
        {
            lock (dbLock)
            {
                return _db.Query<StarsPayment>("SELECT * FROM StarsPaymentTransactions").ToList();
            }
        }

        public StarsPayment? GetById(int id)
        {
            lock (dbLock)
            {
                return _db.QuerySingleOrDefault<StarsPayment>("SELECT * FROM StarsPaymentTransactions WHERE Id = @Id", new { Id = id });
            }
        }

        public int Add(StarsPayment starsPaymentTransaction)
        {
            lock (dbLock)
            {
                var sql = @"INSERT INTO StarsPaymentTransactions (
                                UserId,
                                AmountUsd,
                                AmountStars,
                                CreatedDateTime,
                                IsCompleted,
                                CompletedDateTime)
                            VALUES (
                                @UserId,
                                @AmountUsd,
                                @AmountStars,
                                @CreatedDateTime,
                                @IsCompleted,
                                @CompletedDateTime);
                            SELECT last_insert_rowid();";
                return _db.ExecuteScalar<int>(sql, starsPaymentTransaction);
            }
        }

        public void Update(StarsPayment starsPaymentTransaction)
        {
            lock (dbLock)
            {
                var sql = @"UPDATE StarsPaymentTransactions SET
                                UserId = @UserId,
                                AmountUsd = @AmountUsd,
                                AmountStars = @AmountStars,
                                CreatedDateTime = @CreatedDateTime,
                                IsCompleted = @IsCompleted,
                                CompletedDateTime = @CompletedDateTime
                            WHERE Id = @Id";
                _db.Execute(sql, starsPaymentTransaction);
            }
        }

        public void Delete(int id)
        {
            lock (dbLock)
            {
                var sql = "DELETE FROM StarsPaymentTransactions WHERE Id = @Id";
                _db.Execute(sql, new { Id = id });
            }
        }
    }
}
