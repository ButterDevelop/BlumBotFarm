using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using Dapper;
using System.Data;

namespace BlumBotFarm.Database.Repositories
{
    public class AccountRepository : IRepository<Account>
    {
        private readonly IDbConnection _db;

        public AccountRepository(IDbConnection db)
        {
            _db = db;
        }

        public IEnumerable<Account> GetAll()
        {
            lock (_db)
            {
                return _db.Query<Account>("SELECT * FROM Accounts").ToList();
            }
        }

        public Account? GetById(int id)
        {
            lock (_db)
            {
                return _db.QuerySingleOrDefault<Account>("SELECT * FROM Accounts WHERE Id = @Id", new { Id = id });
            }
        }

        public int Add(Account account)
        {
            lock (_db)
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
                return _db.ExecuteScalar<int>(sql, account);
            }
        }

        public void Update(Account account)
        {
            lock (_db)
            {
                var sql = "UPDATE Accounts SET CustomUsername = @CustomUsername, BlumUsername = @BlumUsername, UserId = @UserId, Balance = @Balance, Tickets = @Tickets, ReferralsCount = @ReferralsCount, ReferralLink = @ReferralLink, AccessToken = @AccessToken, RefreshToken = @RefreshToken, ProviderToken = @ProviderToken, UserAgent = @UserAgent, Proxy = @Proxy, CountryCode = @CountryCode, ProxySellerListId = @ProxySellerListId, TimezoneOffset = @TimezoneOffset, LastStatus = @LastStatus WHERE Id = @Id";
                _db.Execute(sql, account);
            }
        }

        public void Delete(int id)
        {
            lock (_db)
            {
                var sql = "DELETE FROM Accounts WHERE Id = @Id";
                _db.Execute(sql, new { Id = id });
            }
        }
    }
}
