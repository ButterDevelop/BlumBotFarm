namespace BlumBotFarm.Core.Models
{
    public class FeedbackMessage
    {
        public int  Id                            { get; set; }
        public long TelegramUserId                { get; set; }
        public int  UserFeedbackOriginalMessageId { get; set; }
        public int  SupportFeedbackMessageId      { get; set; }
        public bool IsReplied                     { get; set; }
        public int  SupportReplyMessageId         { get; set; }
    }
}
