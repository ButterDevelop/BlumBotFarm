using AutoBlumFarmServer.ApiResponses;
using AutoBlumFarmServer.ApiResponses.TelegramAuthController;
using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using Telegram.Bot;

namespace AutoBlumFarmServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentTransactionController : Controller
    {
        private readonly UserRepository         _userRepository;
        private readonly StarsPaymentRepository _starsPaymentRepository;
        private readonly ReferralRepository     _referralRepository;
        private readonly TelegramBotClient      _telegramBotClient;

        public PaymentTransactionController(UserRepository         userRepository,
                                            StarsPaymentRepository starsPaymentRepository, 
                                            ReferralRepository     referralRepository,
                                            TelegramBotClient      telegramBotClient)
        {
            _userRepository         = userRepository;
            _starsPaymentRepository = starsPaymentRepository;
            _referralRepository     = referralRepository;
            _telegramBotClient      = telegramBotClient;
        }

        // POST: api/Wallet/CreateOrder
        [HttpPost("CreateOrder")]
        [SwaggerResponse(200, "Success. Order was created. Returned the info.")]
        [SwaggerResponse(401, "No auth from user.")]
        [SwaggerResponseExample(200, typeof(StarsPaymentTransactionOkExample))]
        [SwaggerResponseExample(400, typeof(StarsPaymentTransactionBadExample))]
        [SwaggerResponseExample(401, typeof(BadAuthExample))]
        [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status200OK,           "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest,   "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status401Unauthorized, "application/json")]
        public async Task<IActionResult> CreateOrder([FromBody] decimal priceUsd)
        {
            int userId  = Utils.GetUserIdFromClaims(User.Claims, out bool userAuthorized);
            var invoker = _userRepository.GetById(userId);
            if (!userAuthorized || invoker == null || invoker.IsBanned) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            int tgStarsAmount = (int)Math.Round(priceUsd / 2.24M);

            var now = DateTime.Now;
            _starsPaymentRepository.Add(new StarsPayment
            {
                UserId            = userId,
                AmountUsd         = priceUsd,
                AmountStars       = tgStarsAmount,
                CreatedDateTime   = now,
                IsCompleted       = false,
                CompletedDateTime = DateTime.Today
            });
            var payment = _starsPaymentRepository.GetAll().FirstOrDefault(p => p.UserId      == userId && 
                                                                                          p.AmountStars == tgStarsAmount &&
                                                                                          !p.IsCompleted &&
                                                                                          p.CreatedDateTime.Ticks == now.Ticks &&
                                                                                          Math.Abs(p.AmountUsd - priceUsd) < 1e-5M);

            if (payment == null)
            {
                return BadRequest(new ApiMessageResponse()
                {
                    ok      = false,
                    message = "Something went wrong. Please, try again later."
                });
            }

            var message = await _telegramBotClient.SendInvoiceAsync(
                        invoker.TelegramUserId, 
                        Config.Instance.TG_STARS_PAYMENT_TITLE,
                        Config.Instance.TG_STARS_PAYMENT_DESCRIPTION.Replace("{priceUsd}", priceUsd.ToString("N2")),
                        payload:        payment.Id.ToString(),
                        providerToken:  string.Empty,
                        currency:       "XTR",
                        prices:         [new(Config.Instance.TG_STARS_PAYMENT_PRICE_LABEL, tgStarsAmount)],
                        startParameter: invoker.OwnReferralCode,
                        photoUrl:       Config.Instance.TG_STARS_PAYMENT_INVOICE_PHOTO_URL
            );

            if (message == null)
            {
                return BadRequest(new ApiMessageResponse()
                {
                    ok      = false,
                    message = "Something went wrong with Telegram. Please, try again later."
                });
            }

            return Ok(new ApiMessageResponse()
            {
                ok      = true,
                message = "The invoice to top up your balance was sent to you."
            });
        }

    }
}
