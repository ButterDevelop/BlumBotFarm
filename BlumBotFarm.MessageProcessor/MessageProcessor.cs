using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Repositories;
using Telegram.Bot;
using Task = System.Threading.Tasks.Task;

namespace BlumBotFarm.MessageProcessor
{
    public class MessageProcessor
    {
        public static MessageProcessor Instance { get; set; } = new MessageProcessor();

        private readonly TelegramBotClient _telegramBotClient;
        private readonly string[]          _adminUsernames;
        private readonly long[]            _adminChatIds;
        private readonly CancellationToken _cancellationToken;
        private readonly MessageRepository _messageRepository;

        public MessageProcessor()
        {
            _telegramBotClient = new TelegramBotClient("");
            _adminUsernames    = [];
            _adminChatIds      = [];
            _cancellationToken = new CancellationToken();
            using (var db = Database.Database.GetConnection())
            {
                _messageRepository = new MessageRepository(db);
            }
        }

        public MessageProcessor(string token, string[] adminUsernames, long[] adminChatIds, CancellationToken cancellationToken)
        {
            _telegramBotClient  = new TelegramBotClient(token);
            _adminUsernames     = adminUsernames;
            _adminChatIds       = adminChatIds;
            _cancellationToken  = cancellationToken;
            using (var db = Database.Database.GetConnection())
            {
                _messageRepository = new MessageRepository(db);
            }
        }

        public void SendMessageToAdminsInQueue(string text)
        {
            _messageRepository.Add(new Message
            {
                MessageText = text
            });
        }

        public async Task StartAsync()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                await ProcessMessagesAsync();
                await Task.Delay(5000); // Ждем 5 секунд перед следующей проверкой
            }
        }

        private async Task ProcessMessagesAsync()
        {
            var messages = _messageRepository.GetAll();

            foreach (var message in messages)
            {
                foreach (var chatId in _adminChatIds)
                {
                    await _telegramBotClient.SendTextMessageAsync(chatId, message.MessageText, null, Telegram.Bot.Types.Enums.ParseMode.Html);
                }

                _messageRepository.Delete(message.Id);
            }
        }
    }
}
