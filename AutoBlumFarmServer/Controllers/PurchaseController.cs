using AutoBlumFarmServer.SwaggerApiResponses;
using AutoBlumFarmServer.SwaggerApiResponses.PurchaseController;
using AutoBlumFarmServer.Helpers;
using AutoBlumFarmServer.Model;
using BlumBotFarm.Core;
using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Repositories;
using BlumBotFarm.Translation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using BlumBotFarm.CacheUpdater.CacheServices;

namespace AutoBlumFarmServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PurchaseController : Controller
    {
        private readonly AccountRepository _accountRepository;
        private readonly TaskRepository    _taskRepository;
        private readonly UserRepository    _userRepository;
        private readonly IUserCacheService _userCacheService;

        public PurchaseController(AccountRepository accountRepository, TaskRepository taskRepository, UserRepository userRepository,
                                  IUserCacheService userCacheService)
        {
            _accountRepository = accountRepository;
            _taskRepository    = taskRepository;
            _userRepository    = userRepository;
            _userCacheService  = userCacheService;
    }

        private decimal GetNextSlotPriceByNumber(int currentSlotAmount)
        {
            decimal supposedPrice = Config.Instance.MAX_SLOT_PRICE - (currentSlotAmount * Config.Instance.STEP_SLOT_PRICE);
            return supposedPrice >= Config.Instance.MIN_SLOT_PRICE ? supposedPrice : Config.Instance.MIN_SLOT_PRICE;
        }

        private decimal GetNextSeveralSlotsPrice(int currentSlotAmount, int wantedAmount)
        {
            decimal result = 0;
            for (int i = 0; i < wantedAmount; i++)
            {
                result += GetNextSlotPriceByNumber(currentSlotAmount + i);
            }
            return result;
        }

        // POST: api/Purchase/PreBuyAccountsSlots
        [HttpPost("PreBuyAccountsSlots")]
        [SwaggerResponse(200, "Success. Price returned.")]
        [SwaggerResponse(400, "Error. Wrong amount number.")]
        [SwaggerResponse(401, "The only failure status code - no auth from user.")]
        [SwaggerResponseExample(200, typeof(PreBuyAccountsSlotsOkExample))]
        [SwaggerResponseExample(400, typeof(PreBuyAccountsSlotsBadExample))]
        [SwaggerResponseExample(401, typeof(BadAuthExample))]
        [ProducesResponseType(typeof(ApiObjectResponse<PreBuyAccountsSlotsOutputModel>), StatusCodes.Status200OK,           "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse),                                StatusCodes.Status400BadRequest,   "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse),                                StatusCodes.Status401Unauthorized, "application/json")]
        public IActionResult PreBuyAccountsSlots([FromBody] BuyAccountsSlotsInputModel model)
        {
            int userId  = Utils.GetUserIdFromClaims(User.Claims, out bool userAuthorized);
            if (!userAuthorized || !_userCacheService.TryGetFromCache(userId, out User invoker)) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            if (model.amount <= 0 || model.amount > 1000)
            {
                return BadRequest(new ApiMessageResponse
                {
                    ok      = false,
                    message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_THE_AMOUNT_OF_STARS_IS_WRONG%#")
                });
            }

            var currentSlotsAmount = _accountRepository.GetAllFit(a => a.UserId == invoker.Id).Count();
            decimal priceUsd = GetNextSeveralSlotsPrice(currentSlotsAmount, model.amount);

            return Ok(new ApiObjectResponse<PreBuyAccountsSlotsOutputModel>
            {
                ok   = true,
                data = new()
                {
                    price    = priceUsd,
                    discount = (model.amount * Config.Instance.MAX_SLOT_PRICE) - priceUsd
                }
            });
        }

        // POST: api/Purchase/BuyAccountsSlots
        [HttpPost("BuyAccountsSlots")]
        [SwaggerResponse(200, "Success. Successful buy.")]
        [SwaggerResponse(400, "Error. Not enough balance or wrong amount number.")]
        [SwaggerResponse(401, "The only failure status code - no auth from user.")]
        [SwaggerResponseExample(200, typeof(BuyAccountsSlotsOkExample))]
        [SwaggerResponseExample(400, typeof(BuyAccountsSlotsBadExample))]
        [SwaggerResponseExample(401, typeof(BadAuthExample))]
        [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status200OK,           "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest,   "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status401Unauthorized, "application/json")]
        public IActionResult BuyAccountsSlots([FromBody] BuyAccountsSlotsInputModel model)
        {
            int userId  = Utils.GetUserIdFromClaims(User.Claims, out bool userAuthorized);
            if (!userAuthorized || !_userCacheService.TryGetFromCache(userId, out User invoker)) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            if (model.amount <= 0 || model.amount > 1000)
            {
                return BadRequest(new ApiMessageResponse
                {
                    ok      = false,
                    message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_THE_AMOUNT_OF_STARS_IS_WRONG%#")
                });
            }

            var currentSlotsAmount = _accountRepository.GetAllFit(a => a.UserId == invoker.Id).Count();
            decimal priceUsd       = GetNextSeveralSlotsPrice(currentSlotsAmount, model.amount);

            if (invoker.BalanceUSD - priceUsd < 0)
            {
                return BadRequest(new ApiMessageResponse
                {
                    ok      = false,
                    message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_NOT_ENOUGH_MONEY_ON_YOUR_BALANCE%#")
                });
            }

            // Take that money from user. Them owe us
            invoker.BalanceUSD -= priceUsd;
            _userRepository.Update(invoker);

            // Calculating the amount of goods
            var trialAccounts = _accountRepository.GetAllFit(a => a.UserId == invoker.Id && a.IsTrial);
            int trialCount    = Math.Min(model.amount, trialAccounts.Count());
            int slotsAmount   = model.amount - trialCount;

            // Make trial accounts to default
            for (int i = 0; i < trialCount; i++)
            {
                var trialAccount = trialAccounts.ElementAt(i);
                trialAccount.IsTrial = false;
                _accountRepository.Update(trialAccount);
            }

            // Buy slots for remaining
            var startAt = DateTime.Now.AddDays(1);
            for (int i = 0; i < slotsAmount; i++)
            {
                Account account = new()
                {
                    UserId    = userId,
                    UserAgent = HTTPController.GetRandomUserAgent()
                };
                int accountId = _accountRepository.Add(account);

                var taskDailyCheckJob = new BlumBotFarm.Core.Models.Task
                {
                    AccountId          = accountId,
                    TaskType           = "DailyCheckJob",
                    MinScheduleSeconds = 6  * 3600, // 6 hours
                    MaxScheduleSeconds = 10 * 3600, // 10 hours
                    NextRunTime        = startAt
                };
                _taskRepository.Add(taskDailyCheckJob);
            }

            return Ok(new ApiMessageResponse
            {
                ok      = true,
                message = string.Format(
                              TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_YOU_HAVE_BOUGHT_SLOTS_SUCCESSFULLY%#"),
                          model.amount)
            });
        }
    }
}
