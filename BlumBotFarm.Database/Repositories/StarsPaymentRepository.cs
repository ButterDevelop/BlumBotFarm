using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using Dapper;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;

namespace BlumBotFarm.Database.Repositories
{
    public class StarsPaymentRepository : IRepository<StarsPayment>
    {
        private static readonly object _lock = new();
        private readonly string _connectionString;

        public StarsPaymentRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<StarsPayment> GetAll()
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = "SELECT Id, UserId, CAST(AmountUsd AS REAL) AS AmountUsd, AmountStars, CreatedDateTime, IsCompleted, CompletedDateTime FROM StarsPayments";
                    return db.Query<StarsPayment>(sql).ToList();
                }
            }
        }

        public StarsPayment? GetById(int id)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = "SELECT Id, UserId, CAST(AmountUsd AS REAL) AS AmountUsd, AmountStars, CreatedDateTime, IsCompleted, CompletedDateTime FROM StarsPayments WHERE Id = @Id";
                    return db.QuerySingleOrDefault<StarsPayment>(sql, new { Id = id });
                }
            }
        }

        public int Add(StarsPayment starsPayment)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
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
                    return db.ExecuteScalar<int>(sql, starsPayment);
                }
            }
        }

        public void Update(StarsPayment starsPayment)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = @"UPDATE StarsPayments SET
                                    UserId = @UserId,
                                    AmountUsd = @AmountUsd,
                                    AmountStars = @AmountStars,
                                    CreatedDateTime = @CreatedDateTime,
                                    IsCompleted = @IsCompleted,
                                    CompletedDateTime = @CompletedDateTime
                                WHERE Id = @Id";
                    db.Execute(sql, starsPayment);
                }
            }
        }

        public void Delete(int id)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = "DELETE FROM StarsPayments WHERE Id = @Id";
                    db.Execute(sql, new { Id = id });
                }
            }
        }
    }
}
