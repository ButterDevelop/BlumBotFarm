using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Interfaces;
using Dapper;
using System.Data;

namespace BlumBotFarm.Database.Repositories
{
    public class FeedbackMessageRepository : IRepository<FeedbackMessage>
    {
        private readonly IDbConnection _db;

        public FeedbackMessageRepository(IDbConnection db)
        {
            _db = db;
        }

        public IEnumerable<FeedbackMessage> GetAll()
        {
            lock (_db)
            {
                return _db.Query<FeedbackMessage>("SELECT * FROM FeedbackMessages").ToList();
            }
        }

        public FeedbackMessage? GetById(int id)
        {
            lock (_db)
            {
                return _db.QuerySingleOrDefault<FeedbackMessage>("SELECT * FROM FeedbackMessages WHERE Id = @Id", new { Id = id });
            }
        }

        public int Add(FeedbackMessage feedbackMessage)
        {
            lock (_db)
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
                return _db.ExecuteScalar<int>(sql, feedbackMessage);
            }
        }

        public void Update(FeedbackMessage feedbackMessage)
        {
            lock (_db)
            {
                var sql = @"UPDATE FeedbackMessages SET
                                TelegramUserId = @TelegramUserId,
                                UserFeedbackOriginalMessageId = @UserFeedbackOriginalMessageId,
                                SupportFeedbackMessageId = @SupportFeedbackMessageId,
                                IsReplied = @IsReplied,
                                SupportReplyMessageId = @SupportReplyMessageId
                            WHERE Id = @Id";
                _db.Execute(sql, feedbackMessage);
            }
        }

        public void Delete(int id)
        {
            lock (_db)
            {
                var sql = "DELETE FROM FeedbackMessages WHERE Id = @Id";
                _db.Execute(sql, new { Id = id });
            }
        }
    }
}
