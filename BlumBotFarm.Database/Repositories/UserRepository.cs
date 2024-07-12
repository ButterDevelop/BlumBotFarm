using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using Dapper;
using System.Data;

namespace BlumBotFarm.Database.Repositories
{
    public class UserRepository : IRepository<User>
    {
        private static readonly object dbLock = new();

        private readonly IDbConnection _db;

        public UserRepository(IDbConnection db)
        {
            _db = db;
        }

        public IEnumerable<User> GetAll()
        {
            lock (dbLock)
            {
                return _db.Query<User>("SELECT * FROM Users").ToList();
            }
        }

        public User? GetById(int id)
        {
            lock (dbLock)
            {
                return _db.QuerySingleOrDefault<User>("SELECT * FROM Users WHERE Id = @Id", new { Id = id });
            }
        }

        public void Add(User User)
        {
            lock (dbLock)
            {
                var sql = "INSERT INTO Users (TelegramUserId, FirstName, LastName, BalanceUSD, IsBanned, LanguageCode, OwnReferralCode, CreatedAt, PhotoUrl) VALUES (@TelegramUserId, @FirstName, @LastName, @BalanceUSD, @IsBanned, @LanguageCode, @OwnReferralCode, @CreatedAt, @PhotoUrl)";
                _db.Execute(sql, User);
            }
        }

        public void Update(User User)
        {
            lock (dbLock)
            {
                var sql = "UPDATE Users SET TelegramUserId = @TelegramUserId, FirstName = @FirstName, LastName = @LastName, BalanceUSD = @BalanceUSD, IsBanned = @IsBanned, LanguageCode = @LanguageCode, OwnReferralCode = @OwnReferralCode, CreatedAt = @CreatedAt, PhotoUrl = @PhotoUrl WHERE Id = @Id";
                _db.Execute(sql, User);
            }
        }

        public void Delete(int id)
        {
            lock (dbLock)
            {
                var sql = "DELETE FROM Users WHERE Id = @Id";
                _db.Execute(sql, new { Id = id });
            }
        }
    }
}
