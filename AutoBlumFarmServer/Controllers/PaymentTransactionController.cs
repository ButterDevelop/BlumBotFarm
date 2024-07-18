using AutoBlumFarmServer.ApiResponses;
using AutoBlumFarmServer.ApiResponses.TelegramAuthController;
using AutoBlumFarmServer.Helpers;
using AutoBlumFarmServer.Model;
using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Repositories;
using BlumBotFarm.Translation;
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
        private readonly ITelegramBotClient     _telegramBotClient;

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

        // POST: api/PaymentTransaction/CreateOrder
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

            int tgStarsAmount = (int)Math.Round(priceUsd / (decimal)Config.Instance.TG_STARS_PAYMENT_STAR_USD_PRICE);

            var now = DateTime.UtcNow;
            _starsPaymentRepository.Add(new StarsPayment
            {
                UserId            = userId,
                AmountUsd         = priceUsd,
                AmountStars       = tgStarsAmount,
                CreatedDateTime   = now,
                IsCompleted       = false,
                CompletedDateTime = DateTime.UtcNow.Date
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
                    message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_SOMETHING_WENT_WRONG%#")
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
                    message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_SOMETHING_WENT_WRONG_WITH_TELEGRAM%#")
                });
            }

            return Ok(new ApiMessageResponse()
            {
                ok      = true,
                message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_THE_TOPUP_BALANCE_INVOICE_WAS_SENT%#")
            });
        }

        // GET: api/PaymentTransaction/MyTransactions
        [HttpGet("MyTransactions")]
        [SwaggerResponse(200, "Success. The list of transactions is returned.")]
        [SwaggerResponse(401, "No auth from user.")]
        [SwaggerResponseExample(200, typeof(MyPaymentTransactionsOkExample))]
        [SwaggerResponseExample(401, typeof(BadAuthExample))]
        [ProducesResponseType(typeof(ApiObjectResponse<List<StarsPaymentDTO>>), StatusCodes.Status200OK,           "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse),                       StatusCodes.Status401Unauthorized, "application/json")]
        public IActionResult MyTransactions()
        {
            int userId  = Utils.GetUserIdFromClaims(User.Claims, out bool userAuthorized);
            var invoker = _userRepository.GetById(userId);
            if (!userAuthorized || invoker == null || invoker.IsBanned) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            var payments = _starsPaymentRepository.GetAll().Where(p => p.UserId == invoker.Id);

            List<StarsPaymentDTO> results = [];
            foreach (var payment in payments)
            {
                results.Add(new()
                {
                    AmountStars       = payment.AmountStars,
                    AmountUsd         = payment.AmountUsd,
                    CreatedDateTime   = payment.CreatedDateTime,
                    IsCompleted       = payment.IsCompleted,
                    CompletedDateTime = payment.CompletedDateTime,
                });
            }

            return Ok(new ApiObjectResponse<List<StarsPaymentDTO>>()
            {
                ok   = true,
                data = results
            });
        }

        // POST: api/PaymentTransaction/ConvertStarsToUSD
        [HttpPost("ConvertStarsToUSD")]
        [SwaggerResponse(200, "Success. Converted Stars to USD.")]
        [SwaggerResponse(400, "Wrong amount of Stars was specified.")]
        [SwaggerResponse(401, "No auth from user.")]
        [SwaggerResponseExample(200, typeof(ConvertStarsToUsdOkExample))]
        [SwaggerResponseExample(400, typeof(ConvertCurrenciesBadExample))]
        [SwaggerResponseExample(401, typeof(BadAuthExample))]
        [ProducesResponseType(typeof(ApiObjectResponse<decimal>), StatusCodes.Status200OK,           "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse),         StatusCodes.Status400BadRequest,   "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse),         StatusCodes.Status401Unauthorized, "application/json")]
        public IActionResult ConvertStarsToUSD([FromBody] ConvertStarsToUSDInputModel model)
        {
            int userId  = Utils.GetUserIdFromClaims(User.Claims, out bool userAuthorized);
            var invoker = _userRepository.GetById(userId);
            if (!userAuthorized || invoker == null || invoker.IsBanned) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            if (model.stars <= 0)
            {
                return BadRequest(new ApiMessageResponse
                {
                    ok      = false,
                    message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_WRONG_NUMBER_SPECIFIED%#")
                });
            }

            decimal priceUsd = (decimal)Config.Instance.TG_STARS_PAYMENT_STAR_USD_PRICE * model.stars;

            return Ok(new ApiObjectResponse<decimal>()
            {
                ok   = true,
                data = priceUsd
            });
        }

        // POST: api/PaymentTransaction/ConvertUSDToStars
        [HttpPost("ConvertUSDToStars")]
        [SwaggerResponse(200, "Success. Converted USD to Stars.")]
        [SwaggerResponse(400, "Wrong amount of USD was specified.")]
        [SwaggerResponse(401, "No auth from user.")]
        [SwaggerResponseExample(200, typeof(ConvertUsdToStarsOkExample))]
        [SwaggerResponseExample(400, typeof(ConvertCurrenciesBadExample))]
        [SwaggerResponseExample(401, typeof(BadAuthExample))]
        [ProducesResponseType(typeof(ApiObjectResponse<int>), StatusCodes.Status200OK,           "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse),     StatusCodes.Status400BadRequest,   "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse),     StatusCodes.Status401Unauthorized, "application/json")]
        public IActionResult ConvertUSDToStars([FromBody] ConvertUSDToStarsInputModel model)
        {
            int userId  = Utils.GetUserIdFromClaims(User.Claims, out bool userAuthorized);
            var invoker = _userRepository.GetById(userId);
            if (!userAuthorized || invoker == null || invoker.IsBanned) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            if (model.priceUsd <= 0)
            {
                return BadRequest(new ApiMessageResponse
                {
                    ok      = false,
                    message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_WRONG_NUMBER_SPECIFIED%#")
                });
            }

            int tgStarsAmount = (int)Math.Round(model.priceUsd / (decimal)Config.Instance.TG_STARS_PAYMENT_STAR_USD_PRICE);

            return Ok(new ApiObjectResponse<int>()
            {
                ok   = true,
                data = tgStarsAmount
            });
        }
    }
}
