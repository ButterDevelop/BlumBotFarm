using AutoBlumFarmServer.ApiResponses;
using AutoBlumFarmServer.ApiResponses.AccountController;
using AutoBlumFarmServer.DTO;
using AutoBlumFarmServer.Model;
using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace AutoBlumFarmServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountController : Controller
    {
        private readonly AccountRepository     _accountRepository;
        private readonly UserRepository        _userRepository;
        private readonly DailyRewardRepository _dailyRewardRepository;
        private readonly EarningRepository     _earningRepository;

        public AccountController(AccountRepository     accountRepository,     UserRepository    userRepository, 
                                 DailyRewardRepository dailyRewardRepository, EarningRepository earningRepository)
        {
            _accountRepository     = accountRepository;
            _userRepository        = userRepository;
            _dailyRewardRepository = dailyRewardRepository;
            _earningRepository     = earningRepository;
        }

        private bool ValidateUsername(string username)
        {
            Regex regex = new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d]{6,10}$");
            return regex.IsMatch(username);
        }

        // GET: api/Account
        [HttpGet]
        [SwaggerResponse(200, "Success. If `blumAuthData` is empty - means that is a slot.")]
        [SwaggerResponse(401, "The only failure status code - no auth from user.")]
        [SwaggerResponseExample(200, typeof(GetAllAccountsOkExample))]
        [SwaggerResponseExample(401, typeof(BadAuthExample))]
        [ProducesResponseType(typeof(ApiObjectResponse<List<AccountDTO>>), StatusCodes.Status200OK,           "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse),               StatusCodes.Status401Unauthorized, "application/json")]
        public IActionResult GetAllAccounts()
        {
            int userId  = Utils.GetUserIdFromClaims(User.Claims, out bool userAuthorized);
            var invoker = _userRepository.GetById(userId);
            if (!userAuthorized || invoker == null || invoker.IsBanned) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            var invokersAccounts = _accountRepository.GetAll().Where(acc => acc.UserId == userId);

            var  today = DateTime.Now.Date; // Not UTC, here we are not using it because of Quartz in the main project
            var  dailyRewardsToday = _dailyRewardRepository.GetAll().Where(r => r.CreatedAt > today);
            var  earningsToday     = _earningRepository.GetAll().Where(earning => earning.Created > today);

            List<AccountDTO> results = [];
            foreach (var account in invokersAccounts)
            {
                bool tookDailyRewardToday = dailyRewardsToday.Any(r => r.AccountId == account.Id);
                var  todayEarningsSum     = earningsToday.Where(earning => earning.AccountId == account.Id).Sum(earning   => earning.Total);

                results.Add(new AccountDTO
                {
                    Username        = account.Username,
                    Balance         = account.Balance,
                    Tickets         = account.Tickets,
                    ReferralCount   = account.ReferralsCount,
                    ReferralLink    = account.ReferralLink,
                    BlumAuthData    = account.ProviderToken,
                    EarnedToday     = todayEarningsSum,
                    TookDailyReward = tookDailyRewardToday,
                });
            }

            return Ok(new ApiObjectResponse<List<AccountDTO>>
            {
                ok   = true,
                data = results
            });
        }
        
        // GET: api/Account/5
        [HttpGet("{id}")]
        [SwaggerResponse(200, "Success. Once again: if `blumAuthData` is empty - means that is a slot.")]
        [SwaggerResponse(400, "No such account that belongs to our user.")]
        [SwaggerResponse(401, "No auth from user.")]
        [SwaggerResponseExample(200, typeof(GetAccountByIdOkExample))]
        [SwaggerResponseExample(400, typeof(GetAccountById400BadExample))]
        [SwaggerResponseExample(401, typeof(BadAuthExample))]
        [ProducesResponseType(typeof(ApiObjectResponse<AccountDTO>), StatusCodes.Status200OK,           "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse),            StatusCodes.Status400BadRequest,   "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse),            StatusCodes.Status401Unauthorized, "application/json")]
        public IActionResult GetAccountById(int id)
        {
            int userId  = Utils.GetUserIdFromClaims(User.Claims, out bool userAuthorized);
            var invoker = _userRepository.GetById(userId);
            if (!userAuthorized || invoker == null || invoker.IsBanned) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            var account = _accountRepository.GetById(id);
            if (account == null || account.UserId != userId) return BadRequest(new ApiMessageResponse
            {
                ok      = false,
                message = "No such account that belongs to our user."
            });

            var  today = DateTime.Now.Date; // Not UTC, here we are not using it because of Quartz in the main project

            bool tookDailyRewardToday = _dailyRewardRepository.GetAll().Any(r => r.CreatedAt > today && r.AccountId == account.Id);
            var  todayEarningsSum     = _earningRepository
                                            .GetAll()
                                            .Where(earning => earning.Created > today && earning.AccountId == account.Id)
                                            .Sum(earning => earning.Total);

            return Json(new ApiObjectResponse<AccountDTO>
            { 
                ok   = true,
                data = new()
                {
                    Username        = account.Username,
                    Balance         = account.Balance,
                    Tickets         = account.Tickets,
                    ReferralCount   = account.ReferralsCount,
                    ReferralLink    = account.ReferralLink,
                    BlumAuthData    = account.ProviderToken,
                    EarnedToday     = todayEarningsSum,
                    TookDailyReward = tookDailyRewardToday,
                }
            });
        }

        // POST: api/Account/CheckAccountUsername
        [HttpPost("CheckAccountUsername")]
        [SwaggerResponse(200, "Success. The username is available.")]
        [SwaggerResponse(400, "No such account that belongs to our user.")]
        [SwaggerResponse(401, "No auth from user.")]
        [SwaggerResponseExample(200, typeof(CheckAccountUsernameOkExample))]
        [SwaggerResponseExample(400, typeof(CheckAccountUsernameBadExample))]
        [SwaggerResponseExample(401, typeof(BadAuthExample))]
        [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status200OK,           "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest,   "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status401Unauthorized, "application/json")]
        public IActionResult CheckAccountUsername([FromBody] CheckAccountUsernameInputModel model)
        {
            string username = model.username;

            int userId  = Utils.GetUserIdFromClaims(User.Claims, out bool userAuthorized);
            var invoker = _userRepository.GetById(userId);
            if (!userAuthorized || invoker == null || invoker.IsBanned) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            if (!ValidateUsername(username))
            {
                return BadRequest(new ApiMessageResponse
                {
                    ok      = false,
                    message = "Validation failed. Use 6-10 alphanumeric symbols.",
                });
            }

            var account = _accountRepository.GetAll().FirstOrDefault(acc => acc.UserId == userId && acc.Username == username);
            if (account != null) return BadRequest(new ApiMessageResponse
            {
                ok      = false,
                message = "This username is already occupied by one of your accounts."
            });

            return Ok(new ApiMessageResponse
            {
                ok      = true,
                message = "This username for your account is available."
            });
        }

        // PUT: api/Account/5
        [HttpPut("{id}")]
        [SwaggerResponse(200, "Success. The account/slot was updated (only `Username` or `ProviderToken`).")]
        [SwaggerResponse(400, "No such account that belongs to our user.")]
        [SwaggerResponse(401, "No auth from user.")]
        [SwaggerResponseExample(200, typeof(UpdateAccountOkExample))]
        [SwaggerResponseExample(400, typeof(UpdateAccountBadExample))]
        [SwaggerResponseExample(401, typeof(BadAuthExample))]
        [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status200OK,           "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest,   "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status401Unauthorized, "application/json")]
        public IActionResult UpdateAccount(int id, [FromBody] AccountDTO updateAccount)
        {
            int userId  = Utils.GetUserIdFromClaims(User.Claims, out bool userAuthorized);
            var invoker = _userRepository.GetById(userId);
            if (!userAuthorized || invoker == null || invoker.IsBanned) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            var account = _accountRepository.GetById(id);
            if (account == null || account.UserId != userId) return BadRequest(new ApiMessageResponse
            {
                ok      = false,
                message = "No such account that belongs to our user."
            });

            if (!ValidateUsername(updateAccount.Username))
            {
                return BadRequest(new ApiMessageResponse
                {
                    ok      = false,
                    message = "Validation failed. Use 6-10 alphanumeric symbols.",
                });
            }

            var accountCheckUsername = _accountRepository.GetAll().FirstOrDefault(acc => acc.Username == updateAccount.Username);
            if (accountCheckUsername != null) return BadRequest(new ApiMessageResponse
            {
                ok      = false,
                message = "This username is already occupied by your account or someone else's."
            });

            account.Username      = updateAccount.Username;
            account.ProviderToken = updateAccount.BlumAuthData;
            _accountRepository.Update(account);

            return Ok(new ApiMessageResponse
            {
                ok      = true,
                message = "The account was updated successfully."
            });
        }
    }
}
