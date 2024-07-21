using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using Dapper;
using Microsoft.Data.Sqlite;

namespace BlumBotFarm.Database.Repositories
{
    public class WalletPaymentRepository : IRepository<WalletPayment>
    {
        private static readonly object _lock = new();

        private readonly string _connectionString;

        public WalletPaymentRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<WalletPayment> GetAll()
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    return db.Query<WalletPayment>("SELECT * FROM PaymentTransactions").ToList();
                }
            }
        }

        public WalletPayment? GetById(int id)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    return db.QuerySingleOrDefault<WalletPayment>("SELECT * FROM PaymentTransactions WHERE Id = @Id", new { Id = id });
                }
            }
        }

        public int Add(WalletPayment paymentTransaction)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = @"INSERT INTO PaymentTransactions (
                                    AmountUsd,
                                    AutoConversionCurrency,
                                    Description,
                                    ReturnUrl,
                                    FailReturnUrl,
                                    CustomData,
                                    ExternalId,
                                    TimeoutSeconds,
                                    CustomerTelegramId,
                                    WalletOrderId,
                                    Status,
                                    OrderNumber,
                                    CreatedDateTime,
                                    ExpirationDateTime,
                                    CompletedDateTime,
                                    PayLink,
                                    DirectPayLink)
                                VALUES (
                                    @AmountUsd,
                                    @AutoConversionCurrency,
                                    @Description,
                                    @ReturnUrl,
                                    @FailReturnUrl,
                                    @CustomData,
                                    @ExternalId,
                                    @TimeoutSeconds,
                                    @CustomerTelegramId,
                                    @WalletOrderId,
                                    @Status,
                                    @OrderNumber,
                                    @CreatedDateTime,
                                    @ExpirationDateTime,
                                    @CompletedDateTime,
                                    @PayLink,
                                    @DirectPayLink);
                                SELECT last_insert_rowid();";
                    return db.ExecuteScalar<int>(sql, paymentTransaction);
                }
            }
        }

        public void Update(WalletPayment paymentTransaction)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = @"UPDATE PaymentTransactions SET
                                    AmountUsd = @AmountUsd,
                                    AutoConversionCurrency = @AutoConversionCurrency,
                                    Description = @Description,
                                    ReturnUrl = @ReturnUrl,
                                    FailReturnUrl = @FailReturnUrl,
                                    CustomData = @CustomData,
                                    ExternalId = @ExternalId,
                                    TimeoutSeconds = @TimeoutSeconds,
                                    CustomerTelegramId = @CustomerTelegramId,
                                    WalletOrderId = @WalletOrderId,
                                    Status = @Status,
                                    OrderNumber = @OrderNumber,
                                    CreatedDateTime = @CreatedDateTime,
                                    ExpirationDateTime = @ExpirationDateTime,
                                    CompletedDateTime = @CompletedDateTime,
                                    PayLink = @PayLink,
                                    DirectPayLink = @DirectPayLink
                                WHERE Id = @Id";
                    db.Execute(sql, paymentTransaction);
                }
            }
        }

        public void Delete(int id)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = "DELETE FROM PaymentTransactions WHERE Id = @Id";
                    db.Execute(sql, new { Id = id });
                }
            }
        }
    }
}