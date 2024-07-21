using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using Dapper;
using Microsoft.Data.Sqlite;

namespace BlumBotFarm.Database.Repositories
{
    public class UserRepository : IRepository<User>
    {
        private static readonly object _lock = new();
        private readonly string _connectionString;

        public UserRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<User> GetAll()
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    return db.Query<User>("SELECT * FROM Users").ToList();
                }
            }
        }

        public User? GetById(int id)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    return db.QuerySingleOrDefault<User>("SELECT * FROM Users WHERE Id = @Id", new { Id = id });
                }
            }
        }

        public int Add(User user)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = @"INSERT INTO Users 
                                (TelegramUserId, FirstName, LastName, BalanceUSD, IsBanned, LanguageCode, OwnReferralCode, CreatedAt) VALUES 
                                (@TelegramUserId, @FirstName, @LastName, @BalanceUSD, @IsBanned, @LanguageCode, @OwnReferralCode, @CreatedAt); 
                                SELECT last_insert_rowid();";
                    return db.ExecuteScalar<int>(sql, user);
                }
            }
        }

        public void Update(User user)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = @"UPDATE Users SET 
                                TelegramUserId = @TelegramUserId, 
                                FirstName = @FirstName, 
                                LastName = @LastName, 
                                BalanceUSD = @BalanceUSD, 
                                IsBanned = @IsBanned, 
                                LanguageCode = @LanguageCode, 
                                OwnReferralCode = @OwnReferralCode, 
                                CreatedAt = @CreatedAt 
                                WHERE Id = @Id";
                    db.Execute(sql, user);
                }
            }
        }

        public void Delete(int id)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = "DELETE FROM Users WHERE Id = @Id";
                    db.Execute(sql, new { Id = id });
                }
            }
        }
    }
}