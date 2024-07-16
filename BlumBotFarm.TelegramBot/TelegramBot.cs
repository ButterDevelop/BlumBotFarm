using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Repositories;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;
using Task = System.Threading.Tasks.Task;

namespace BlumBotFarm.TelegramBot
{
    public class TelegramBot
    {
        private const int REFERRAL_CODE_STRING_LENGTH = 10;
        private readonly static Random random = new();
        private const string ALPHABET_NUMERIC_CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        private readonly ITelegramBotClient        botClient;
        private readonly string[]                  adminUsernames;
        private readonly long[]                    adminChatIds;
        private readonly double                    starPriceUsd;
        private readonly int                       referralBalanceBonusPercent;
        private readonly UserRepository            userRepository;
        private readonly StarsPaymentRepository    starsPaymentRepository;
        private readonly ReferralRepository        referralRepository;
        private readonly FeedbackMessageRepository feedbackMessageRepository;
        private readonly string                    serverDomain;
        private readonly string                    publicBotName;
        private readonly long                      techSupportGroupChatId;

        public TelegramBot(string token, string[] adminUsernames, long[] adminChatIds, double starPriceUsd, 
                           int referralBalanceBonusPercent, string serverDomain, string publicBotName, long techSupportGroupChatId)
        {
            botClient           = new TelegramBotClient(token);
            this.adminUsernames = adminUsernames;
            this.adminChatIds   = adminChatIds;
            this.starPriceUsd   = starPriceUsd;
            using (var db = Database.Database.GetConnection())
            {
                userRepository            = new UserRepository(db);
                starsPaymentRepository    = new StarsPaymentRepository(db);
                referralRepository        = new ReferralRepository(db);
                feedbackMessageRepository = new FeedbackMessageRepository(db);
            }
            this.referralBalanceBonusPercent = referralBalanceBonusPercent;
            this.serverDomain                = serverDomain;
            this.publicBotName               = publicBotName;
            this.techSupportGroupChatId      = techSupportGroupChatId;
        }

        public void Start()
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = [UpdateType.Message, UpdateType.PreCheckoutQuery]
            };
            botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, cancellationToken: CancellationToken.None);
        }

        public async Task SendMessageToAdmins(string message)
        {
            foreach (var adminChatId in adminChatIds)
            {
                await botClient.SendTextMessageAsync(adminChatId, message, null, ParseMode.Html);
            }
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.PreCheckoutQuery && update.PreCheckoutQuery != null)
            {
                await HandlePreCheckoutQuery(update.PreCheckoutQuery);
                return;
            }

            if (update.Message != null)
            {
                if (update.Message.Type == MessageType.SuccessfulPayment && update.Message.SuccessfulPayment != null)
                {
                    await HandleSuccessfulPayment(update.Message);
                    return;
                }

                if (update.Type == UpdateType.Message && update.Message.Type == MessageType.Text)
                {
                    var message = update.Message;
                    if (message.From is null) return;

                    await HandleUserMessage(message);
                }
            }
        }

        private async Task HandlePreCheckoutQuery(PreCheckoutQuery preCheckoutQuery)
        {
            try
            {
                if (preCheckoutQuery.Currency == "XTR")
                {
                    // Если всё хорошо, подтвердите pre_checkout_query
                    await botClient.AnswerPreCheckoutQueryAsync(
                        preCheckoutQuery.Id
                    );
                }
                else
                {
                    // Error, user is not using stars
                    await botClient.AnswerPreCheckoutQueryAsync(
                        preCheckoutQuery.Id,
                        errorMessage: "We are so sorry, but for now we are using only Telegram Stars as payment method.\n" +
                                      "If you want to discuss it or offer some other ways, than please consider using command /feedback"
                    );
                }
            }
            catch (Exception ex)
            {
                // В случае ошибки, отклоните pre_checkout_query с описанием причины
                await botClient.AnswerPreCheckoutQueryAsync(
                    preCheckoutQuery.Id,
                    errorMessage: "Sorry, there was an error processing your order. Please, try later."
                );

                Log.Error($"Error in HandlePreCheckoutQuery: {ex.Message}");
            }
        }

        private async Task HandleSuccessfulPayment(Telegram.Bot.Types.Message message)
        {
            var payment = message.SuccessfulPayment;
            if (payment == null)
            {
                Log.Error($"Can't handle successful payment! Payment is NULL. Chat Id: {message.Chat.Id}");
                return;
            }

            if (!int.TryParse(payment.InvoicePayload, out int payloadStarsPaymentId))
            {
                Log.Error($"Can't handle successful payment! Can't parse Payment ID from Invoice payload. Chat Id: {message.Chat.Id}");
                return;
            }

            var starsPaymentDb = starsPaymentRepository.GetById(payloadStarsPaymentId);
            if (starsPaymentDb == null)
            {
                Log.Error($"Can't handle successful payment! starsPaymentDb is NULL. Chat Id: {message.Chat.Id}");
                return;
            }

            var user = userRepository.GetById(starsPaymentDb.UserId);
            if (user == null)
            {
                Log.Error($"Can't handle successful payment! Can't find user in DB! Chat Id: {message.Chat.Id}");
                return;
            }

            starsPaymentDb.CompletedDateTime = DateTime.Now;
            starsPaymentDb.IsCompleted = true;
            starsPaymentRepository.Update(starsPaymentDb);

            int    amountStars = payment.TotalAmount;
            double amountUsd   = amountStars * starPriceUsd;

            // Change user's balance
            user.BalanceUSD += (decimal)amountUsd;
            userRepository.Update(user);

            // Notify user
            await botClient.SendTextMessageAsync(message.Chat, $"Your balance was <b>successfully</b> increased by <b>{amountUsd:N2}$</b>.\n" +
                                                               $"Thank <b>you</b> very much!", 
                                                 null, ParseMode.Html);

            string messageToAdmins = "<b>We got DONATED!!!</b>\nUser <b>" + (message.From is null ? "<i>Unknown username</i>" : message.From.Username) +
                                     "</b> toped up their balance with " +
                                     $"<b>{amountStars}</b> stars (<b>~{amountUsd:N4}</b>$).";

            var hostOfOurUserRef = referralRepository.GetAll().FirstOrDefault(r => r.DependentUserId == user.Id);
            if (hostOfOurUserRef != null)
            {
                var hostUser = userRepository.GetById(hostOfOurUserRef.HostUserId);
                if (hostUser != null)
                {
                    var increaseBy = (decimal)Math.Round(amountUsd * (referralBalanceBonusPercent / 100.0), 2);

                    hostUser.BalanceUSD += increaseBy;
                    userRepository.Update(hostUser);

                    // Notify host user
                    await botClient.SendTextMessageAsync(hostUser.TelegramUserId,
                                                            $"Your balance was <b>successfully</b> increased by <b>{amountUsd:N2}$</b>.\n" +
                                                            $"That happened because your referral <b>{user.FirstName + " " + user.LastName}</b> " +
                                                            "toped up their balance! Thanks to them!\n" +
                                                            $"And thank <b>you</b> very much!",
                                                         null, ParseMode.Html);

                    messageToAdmins += "\nAlso referrals balance was toped up.\n" +
                                       $"Referral's id: <b>{user.Id}</b>, chat id: <b>{user.TelegramUserId}</b>, " +
                                       $"increased by <b>{increaseBy:N2}</b>$ (bonus percent is <b>{referralBalanceBonusPercent}%</b>).";

                    Log.Information($"Also referrals balance was toped up. Chat Id: {message.Chat.Id}. " + 
                                    $"Referral's id: {user.Id}, chat id: {user.TelegramUserId}, " +
                                    $"increased by {increaseBy:N2}$ (bonus percent is {referralBalanceBonusPercent}%).");
                }
                else
                {
                    Log.Warning($"Can't give bonus to the host user. No user in DB. Chat Id: {message.Chat.Id}");
                }
            }
            else
            {
                Log.Information($"This user has no Host Referral to give them bonus. Chat Id: {message.Chat.Id}");
            }

            // Notify admins
            await SendMessageToAdmins(messageToAdmins);

            // Logs
            Log.Information($"Successful payment received: {payment.TotalAmount} {payment.Currency}. Chat Id: {message.Chat.Id}");
        }

        private async Task HandleUserMessage(Telegram.Bot.Types.Message message)
        {
            if (message.Text is null || message.From is null) return;

            var parts   = message.Text.Split(' ');
            var command = parts[0].ToLower();

            Log.Information($"Command called by {message.From.Username}: {message.Text}");

            if (command.Contains('@'))
            {
                var splitted = command.Split('@');

                if (splitted.Length >= 2)
                {
                    command = splitted[0];

                    var botCalledName = splitted[1];
                    if (!botCalledName.Equals(publicBotName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        Log.Warning("It is not our call. Returning.");
                        return;
                    }
                }
            }

            if (message.Chat.Id == techSupportGroupChatId)
            {
                if (message.ReplyToMessage != null)
                {
                    // Replying to user's message
                    var usersFeedback = feedbackMessageRepository
                                                .GetAll()
                                                .FirstOrDefault(f => f.SupportFeedbackMessageId == message.ReplyToMessage.MessageId);
                    if (usersFeedback is null)
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Cannot find the message you replied in the database.",
                                                             null, ParseMode.Html);
                        return;
                    }

                    usersFeedback.IsReplied = true;
                    usersFeedback.SupportReplyMessageId = message.MessageId;
                    feedbackMessageRepository.Update(usersFeedback);

                    string userMessageReplyFeedback = "<b>You got feedback on your message!</b>\n" +
                                                      "Thanks for your patience. Here is the message.\n" +
                                                      $"<blockquote expandable>{message.Text}</blockquote>";
                    try
                    {
                        await botClient.SendTextMessageAsync(usersFeedback.TelegramUserId, userMessageReplyFeedback, null, ParseMode.Html,
                                                             replyToMessageId: usersFeedback.UserFeedbackOriginalMessageId);
                    }
                    catch
                    {
                        await botClient.SendTextMessageAsync(usersFeedback.TelegramUserId, userMessageReplyFeedback, null, ParseMode.Html);
                    }

                    await botClient.SendTextMessageAsync(techSupportGroupChatId, "Your reply to user's feedback was sent successfully.",
                                                         null, ParseMode.Html);
                }
            }
            else
            {
                switch (command)
                {
                    case "/start":
                        var users = userRepository.GetAll();
                        var user  = users.FirstOrDefault(u => u.TelegramUserId == message.Chat.Id);
                        if (user == null)
                        {
                            string usersReferralCode;
                            do
                            {
                                usersReferralCode = RandomString(REFERRAL_CODE_STRING_LENGTH);
                            } while (users.FirstOrDefault(u => u.OwnReferralCode == usersReferralCode) != null);

                            user = new Core.Models.User()
                            {
                                BalanceUSD = 0M,
                                TelegramUserId = message.Chat.Id,
                                FirstName = message.From.FirstName,
                                LastName = message.From.LastName ?? "",
                                IsBanned = false,
                                LanguageCode = message.From.LanguageCode ?? "en",
                                OwnReferralCode = usersReferralCode,
                                CreatedAt = DateTime.Now,
                                PhotoUrl = string.Empty
                            };
                            userRepository.Add(user);

                            user = userRepository.GetAll().FirstOrDefault(acc => acc.TelegramUserId == message.Chat.Id);
                            if (user == null)
                            {
                                Log.Error($"Command /start, chat id {message.Chat.Id}: can't get user from the DB while creating.");
                                return;
                            }
                        }

                        if (parts.Length == 2)
                        {
                            var hostReferralCode = parts[1];

                            var hostUser     = users.FirstOrDefault(u => u.OwnReferralCode == hostReferralCode && u.Id != user.Id);
                            if (hostUser != null)
                            {
                                var referralUser = referralRepository.GetAll().FirstOrDefault(u => u.DependentUserId == user.Id);
                                if (referralUser == null)
                                {
                                    var referral = new Referral
                                    {
                                        HostUserId      = hostUser.Id,
                                        DependentUserId = user.Id
                                    };
                                    referralRepository.Add(referral);
                                }
                            }
                        }

                        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithWebApp("Open Mini App",
                            new WebAppInfo { Url = serverDomain })
                        }
                    });

                        await botClient.SendTextMessageAsync(
                            message.Chat,
                            "Hi! Click the button below to open out Mini App 💵",
                            replyMarkup: inlineKeyboard,
                            parseMode: ParseMode.Html
                        );
                        break;
                    case "/feedback":
                    case "/paysupport":
                        var feedbackMessage = message.Text.Replace("/feedback", "").Replace("/paysupport", "").Trim();

                        if (string.IsNullOrEmpty(feedbackMessage))
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "Your message is <b>empty</b>!", null, ParseMode.Html);
                            return;
                        }

                        string messageType  = command == "/paysupport" ? "PAY SUPPORT" : "feedback";

                        string languageCode = string.IsNullOrEmpty(message.From.LanguageCode) ? "-" : message.From.LanguageCode;
                        string username     = string.IsNullOrEmpty(message.From.Username) ? "Unknown username" : $"@{message.From.Username}";
                        string fullName     = $"First name: {message.From.FirstName}" +
                                          (message.From.LastName is null ? "" : $", last name: {message.From.LastName}");
                        string textMessageToSendToSupport = $"We got {messageType} from <b>{username}</b> (<i>{fullName}</i>, TG id is <code>{message.Chat.Id}</code>, " +
                                                            $"lang: <b>{languageCode}</b>):\n" +
                                                            $"<blockquote expandable>{feedbackMessage}</blockquote>";

                        var sentMessageToSupport = await botClient.SendTextMessageAsync(techSupportGroupChatId, textMessageToSendToSupport,
                                                                                    null, ParseMode.Html);

                        feedbackMessageRepository.Add(new FeedbackMessage
                        {
                            TelegramUserId                = message.Chat.Id,
                            UserFeedbackOriginalMessageId = message.MessageId,
                            SupportFeedbackMessageId      = sentMessageToSupport.MessageId,
                            IsReplied                     = false,
                            SupportReplyMessageId         = null
                        });

                        await botClient.SendTextMessageAsync(message.Chat,
                                                                (command == "/paysupport" 
                                                                  ? "<b>Thanks! We'll help you!</b>" 
                                                                  : "<b>Thanks for your feedback!</b>") +
                                                                "\nYour message was successfully sent to us!\n" +
                                                                "We'll answer you as soon as possible. You will get the message to this chat.",
                                                             null, ParseMode.Html);

                        break;
                }
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Log.Error($"Telegram Bot HandleErrorAsync: {exception}");
            return Task.CompletedTask;
        }

        public static string RandomString(int length)
        {
            return new string(Enumerable.Repeat(ALPHABET_NUMERIC_CHARS, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
