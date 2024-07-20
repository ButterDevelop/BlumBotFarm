using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using Dapper;
using System.Data;

namespace BlumBotFarm.Database.Repositories
{
    public class StarsPaymentRepository : IRepository<StarsPayment>
    {
        private readonly IDbConnection _db;

        public StarsPaymentRepository(IDbConnection db)
        {
            _db = db;
        }

        public IEnumerable<StarsPayment> GetAll()
        {
            lock (_db)
            {
                return _db.Query<StarsPayment>("SELECT * FROM StarsPayments").ToList();
            }
        }

        public StarsPayment? GetById(int id)
        {
            lock (_db)
            {
                return _db.QuerySingleOrDefault<StarsPayment>("SELECT * FROM StarsPayments WHERE Id = @Id", new { Id = id });
            }
        }

        public int Add(StarsPayment starsPaymentTransaction)
        {
            lock (_db)
            {
                var sql = @"INSERT INTO StarsPayments (
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
            lock (_db)
            {
                var sql = @"UPDATE StarsPayments SET
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
            lock (_db)
            {
                var sql = "DELETE FROM StarsPayments WHERE Id = @Id";
                _db.Execute(sql, new { Id = id });
            }
        }
    }
}
