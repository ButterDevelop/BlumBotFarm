using AutoBlumFarmServer.ApiResponses;
using AutoBlumFarmServer.ApiResponses.PurchaseController;
using AutoBlumFarmServer.Model;
using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Repositories;
using BlumBotFarm.GameClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

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

        public PurchaseController(AccountRepository accountRepository, TaskRepository taskRepository, UserRepository userRepository)
        {
            _accountRepository = accountRepository;
            _taskRepository    = taskRepository;
            _userRepository    = userRepository;
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
            var invoker = _userRepository.GetById(userId);
            if (!userAuthorized || invoker == null || invoker.IsBanned) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            if (model.amount <= 0)
            {
                return BadRequest(new ApiMessageResponse
                {
                    ok      = false,
                    message = "The amount of slots you specified is wrong."
                });
            }

            decimal priceUsd = model.amount * Config.Instance.ACCOUNT_SLOT_PRICE;

            return Ok(new ApiObjectResponse<PreBuyAccountsSlotsOutputModel>
            {
                ok   = true,
                data = new()
                {
                    price    = priceUsd,
                    discount = 0M
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
            var invoker = _userRepository.GetById(userId);
            if (!userAuthorized || invoker == null || invoker.IsBanned) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            if (model.amount <= 0)
            {
                return BadRequest(new ApiMessageResponse
                {
                    ok      = false,
                    message = "The amount of slots you specified is wrong."
                });
            }

            if (invoker.BalanceUSD - (model.amount * Config.Instance.ACCOUNT_SLOT_PRICE) < 0)
            {
                return BadRequest(new ApiMessageResponse
                {
                    ok      = false,
                    message = "Not enough money on your balance."
                });
            }

            invoker.BalanceUSD -= model.amount * Config.Instance.ACCOUNT_SLOT_PRICE;
            _userRepository.Update(invoker);

            var startAt = DateTime.Now.AddDays(1);
            for (int i = 0; i < model.amount; i++)
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
                message = $"You have bought {model.amount} slot" + (model.amount == 1 ? "" : "s") + " successfully!"
            });
        }
    }
}
