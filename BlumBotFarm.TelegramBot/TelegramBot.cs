using BlumBotFarm.Database.Repositories;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.Payments;
using Task = System.Threading.Tasks.Task;

namespace BlumBotFarm.TelegramBot
{
    public class TelegramBot
    {
        private readonly ITelegramBotClient     botClient;
        private readonly string[]               adminUsernames;
        private readonly long[]                 adminChatIds;
        private readonly double                 starPriceUsd;
        private readonly int                    referralBalanceBonusPercent;
        private readonly UserRepository         userRepository;
        private readonly StarsPaymentRepository starsPaymentRepository;
        private readonly ReferralRepository     referralRepository;

        public TelegramBot(string token, string[] adminUsernames, long[] adminChatIds, double starPriceUsd, int referralBalanceBonusPercent)
        {
            botClient           = new TelegramBotClient(token);
            this.adminUsernames = adminUsernames;
            this.adminChatIds   = adminChatIds;
            this.starPriceUsd   = starPriceUsd;
            using (var db = Database.Database.GetConnection())
            {
                userRepository         = new UserRepository(db);
                starsPaymentRepository = new StarsPaymentRepository(db);
                referralRepository     = new ReferralRepository(db);
            }
            this.referralBalanceBonusPercent = referralBalanceBonusPercent;
        }

        public void Start()
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = [UpdateType.Message, UpdateType.PreCheckoutQuery, UpdateType.SuccessfulPayment]
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
            if (update.PreCheckoutQuery != null)
            {
                await HandlePreCheckoutQuery(update.PreCheckoutQuery);
                return;
            }

            if (update.Message != null)
            {
                if (update.Message.SuccessfulPayment != null)
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

        private async Task HandleSuccessfulPayment(Message message)
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

        private async Task HandleUserMessage(Message message)
        {
            if (message.Text is null || message.From is null) return;

            var parts   = message.Text.Split(' ');
            var command = parts[0].ToLower();

            Log.Information($"Command called by {message.From.Username}: {message.Text}");

            switch (command)
            {
                case "/start":
                    await botClient.SendTextMessageAsync(message.Chat, "Hi!",
                                                                       null, ParseMode.Html);
                    break;
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Log.Error($"Telegram Bot HandleErrorAsync: {exception}");
            return Task.CompletedTask;
        }
    }
}
