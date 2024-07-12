using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using Dapper;
using System.Data;

namespace BlumBotFarm.Database.Repositories
{
    public class WalletPaymentRepository : IRepository<WalletPayment>
    {
        private static readonly object dbLock = new();

        private readonly IDbConnection _db;

        public WalletPaymentRepository(IDbConnection db)
        {
            _db = db;
        }

        public IEnumerable<WalletPayment> GetAll()
        {
            lock (dbLock)
            {
                return _db.Query<WalletPayment>("SELECT * FROM PaymentTransactions").ToList();
            }
        }

        public WalletPayment? GetById(int id)
        {
            lock (dbLock)
            {
                return _db.QuerySingleOrDefault<WalletPayment>("SELECT * FROM PaymentTransactions WHERE Id = @Id", new { Id = id });
            }
        }

        public void Add(WalletPayment paymentTransaction)
        {
            lock (dbLock)
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
                                @DirectPayLink)";
                _db.Execute(sql, paymentTransaction);
            }
        }

        public void Update(WalletPayment paymentTransaction)
        {
            lock (dbLock)
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
                _db.Execute(sql, paymentTransaction);
            }
        }

        public void Delete(int id)
        {
            lock (dbLock)
            {
                var sql = "DELETE FROM PaymentTransactions WHERE Id = @Id";
                _db.Execute(sql, new { Id = id });
            }
        }
    }
}
