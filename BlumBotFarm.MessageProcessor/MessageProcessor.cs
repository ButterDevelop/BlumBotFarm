using BlumBotFarm.Database.Repositories;
using Telegram.Bot;
using Task = System.Threading.Tasks.Task;

namespace BlumBotFarm.MessageProcessor
{
    public class MessageProcessor
    {
        public static MessageProcessor? Instance { get; set; }

        private readonly ITelegramBotClient _adminTelegramBotClient;
        private readonly ITelegramBotClient _userTelegramBotClient;
        private readonly long[]             _adminChatIds;
        private readonly CancellationToken  _cancellationToken;
        private readonly MessageRepository  _messageRepository;

        public MessageProcessor(TelegramBotClient adminTelegramBotClient, TelegramBotClient userTelegramBotClient, 
                                long[] adminChatIds, CancellationToken cancellationToken)
        {
            _adminTelegramBotClient = adminTelegramBotClient;
            _userTelegramBotClient  = userTelegramBotClient;
            _adminChatIds           = adminChatIds;
            _cancellationToken      = cancellationToken;
            _messageRepository      = new MessageRepository(Database.Database.ConnectionString);
        }

        public void SendMessageToUserInQueue(long chatId, string text, bool isSilent)
        {
            _messageRepository.Add(new Core.Models.Message
            {
                ChatId      = chatId,
                MessageText = text,
                CreatedAt   = DateTime.Now,
                IsSilent    = isSilent
            });
        }

        public void SendMessageToAdminsInQueue(string text, bool isSilent)
        {
            _messageRepository.Add(new Core.Models.Message
            {
                ChatId      = 0,
                MessageText = text,
                CreatedAt   = DateTime.Now,
                IsSilent    = isSilent
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
                if (message.ChatId == 0)
                {
                    foreach (var chatId in _adminChatIds)
                    {
                        await _adminTelegramBotClient.SendTextMessageAsync(chatId, message.MessageText, null,
                                                                      Telegram.Bot.Types.Enums.ParseMode.Html, disableNotification: message.IsSilent);
                    }
                }
                else
                {
                    await _userTelegramBotClient.SendTextMessageAsync(message.ChatId, message.MessageText, null,
                                                                  Telegram.Bot.Types.Enums.ParseMode.Html, disableNotification: message.IsSilent);
                }

                _messageRepository.Delete(message.Id);
            }
        }
    }
}
