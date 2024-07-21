using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using Dapper;
using Microsoft.Data.Sqlite;

namespace BlumBotFarm.Database.Repositories
{
    public class FeedbackMessageRepository : IRepository<FeedbackMessage>
    {
        private static readonly object _lock = new();
        private readonly string _connectionString;

        public FeedbackMessageRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<FeedbackMessage> GetAll()
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    return db.Query<FeedbackMessage>("SELECT * FROM FeedbackMessages").ToList();
                }
            }
        }

        public FeedbackMessage? GetById(int id)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    return db.QuerySingleOrDefault<FeedbackMessage>("SELECT * FROM FeedbackMessages WHERE Id = @Id", new { Id = id });
                }
            }
        }

        public int Add(FeedbackMessage feedbackMessage)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = @"INSERT INTO FeedbackMessages (
                                    TelegramUserId,
                                    UserFeedbackOriginalMessageId,
                                    SupportFeedbackMessageId,
                                    IsReplied,
                                    SupportReplyMessageId)
                                VALUES (
                                    @TelegramUserId,
                                    @UserFeedbackOriginalMessageId,
                                    @SupportFeedbackMessageId,
                                    @IsReplied,
                                    @SupportReplyMessageId);
                                SELECT last_insert_rowid();";
                    return db.ExecuteScalar<int>(sql, feedbackMessage);
                }
            }
        }

        public void Update(FeedbackMessage feedbackMessage)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = @"UPDATE FeedbackMessages SET
                                    TelegramUserId = @TelegramUserId,
                                    UserFeedbackOriginalMessageId = @UserFeedbackOriginalMessageId,
                                    SupportFeedbackMessageId = @SupportFeedbackMessageId,
                                    IsReplied = @IsReplied,
                                    SupportReplyMessageId = @SupportReplyMessageId
                                WHERE Id = @Id";
                    db.Execute(sql, feedbackMessage);
                }
            }
        }

        public void Delete(int id)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = "DELETE FROM FeedbackMessages WHERE Id = @Id";
                    db.Execute(sql, new { Id = id });
                }
            }
        }
    }
}