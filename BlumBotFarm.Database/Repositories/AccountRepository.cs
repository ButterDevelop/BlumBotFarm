using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using Dapper;
using Microsoft.Data.Sqlite;

namespace BlumBotFarm.Database.Repositories
{
    public class AccountRepository : IRepository<Account>
    {
        private static readonly object _lock = new();
        private readonly string _connectionString;

        public AccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<Account> GetAll()
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    return db.Query<Account>("SELECT * FROM Accounts").ToList();
                }
            }
        }

        public Account? GetById(int id)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    return db.QuerySingleOrDefault<Account>("SELECT * FROM Accounts WHERE Id = @Id", new { Id = id });
                }
            }
        }

        public int Add(Account account)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = @"INSERT INTO Accounts (
                                    CustomUsername, BlumUsername, UserId, Balance, Tickets, ReferralsCount, ReferralLink,
                                    AccessToken, RefreshToken, ProviderToken, UserAgent, Proxy, CountryCode, ProxySellerListId, TimezoneOffset,
                                    LastStatus)
                                VALUES (
                                    @CustomUsername, @BlumUsername, @UserId, @Balance, @Tickets, @ReferralsCount, @ReferralLink,
                                    @AccessToken, @RefreshToken, @ProviderToken, @UserAgent, @Proxy, @CountryCode, @ProxySellerListId, @TimezoneOffset, 
                                    @LastStatus);
                                SELECT last_insert_rowid();";
                    return db.ExecuteScalar<int>(sql, account);
                }
            }
        }

        public void Update(Account account)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = @"UPDATE Accounts SET
                                    CustomUsername = @CustomUsername,
                                    BlumUsername = @BlumUsername,
                                    UserId = @UserId,
                                    Balance = @Balance,
                                    Tickets = @Tickets,
                                    ReferralsCount = @ReferralsCount,
                                    ReferralLink = @ReferralLink,
                                    AccessToken = @AccessToken,
                                    RefreshToken = @RefreshToken,
                                    ProviderToken = @ProviderToken,
                                    UserAgent = @UserAgent,
                                    Proxy = @Proxy,
                                    CountryCode = @CountryCode,
                                    ProxySellerListId = @ProxySellerListId,
                                    TimezoneOffset = @TimezoneOffset,
                                    LastStatus = @LastStatus
                                WHERE Id = @Id";
                    db.Execute(sql, account);
                }
            }
        }

        public void Delete(int id)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = "DELETE FROM Accounts WHERE Id = @Id";
                    db.Execute(sql, new { Id = id });
                }
            }
        }
    }
}