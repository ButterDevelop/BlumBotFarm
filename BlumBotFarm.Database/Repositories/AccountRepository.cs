using BlumBotFarm.Core.Models;
using Dapper;
using System.Data;

namespace BlumBotFarm.Database.Repositories
{
    public class AccountRepository
    {
        private static readonly object dbLock = new object();

        private readonly IDbConnection _db;

        public AccountRepository(IDbConnection db)
        {
            _db = db;
        }

        public IEnumerable<Account> GetAll()
        {
            lock (dbLock)
            {
                return _db.Query<Account>("SELECT * FROM Accounts").ToList();
            }
        }

        public Account? GetById(int id)
        {
            lock (dbLock)
            {
                return _db.QuerySingleOrDefault<Account>("SELECT * FROM Accounts WHERE Id = @Id", new { Id = id });
            }
        }

        public void Add(Account account)
        {
            lock (dbLock)
            {
                var sql = "INSERT INTO Accounts (Username, Balance, Tickets, AccessToken, RefreshToken, UserAgent, Proxy, TimezoneOffset) VALUES (@Username, @Balance, @Tickets, @AccessToken, @RefreshToken, @UserAgent, @Proxy, @TimezoneOffset)";
                _db.Execute(sql, account);
            }
        }

        public void Update(Account account)
        {
            lock (dbLock)
            {
                var sql = "UPDATE Accounts SET Username = @Username, Balance = @Balance, Tickets = @Tickets, AccessToken = @AccessToken, RefreshToken = @RefreshToken, UserAgent = @UserAgent, Proxy = @Proxy, TimezoneOffset = @TimezoneOffset WHERE Id = @Id";
                _db.Execute(sql, account);
            }
        }

        public void Delete(int id)
        {
            lock (dbLock)
            {
                var sql = "DELETE FROM Accounts WHERE Id = @Id";
                _db.Execute(sql, new { Id = id });
            }
        }
    }
}
