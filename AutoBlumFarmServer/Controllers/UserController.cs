using AutoBlumFarmServer.ApiResponses;
using AutoBlumFarmServer.ApiResponses.UserController;
using AutoBlumFarmServer.DTO;
using AutoBlumFarmServer.Helpers;
using AutoBlumFarmServer.Model;
using BlumBotFarm.Database.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using System.Text.RegularExpressions;

namespace AutoBlumFarmServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : Controller
    {
        private readonly UserRepository     _userRepository;
        private readonly AccountRepository  _accountRepository;
        private readonly ReferralRepository _referralRepository;

        public UserController(UserRepository userRepository, AccountRepository accountRepository, ReferralRepository referralRepository)
        {
            _userRepository     = userRepository;
            _accountRepository  = accountRepository;
            _referralRepository = referralRepository;
        }

        // GET: api/User/Me
        [HttpGet("Me")]
        [SwaggerResponse(200, "Success. The info about our user.")]
        [SwaggerResponse(401, "No auth from user.")]
        [SwaggerResponseExample(200, typeof(AboutMeUserInfoOkExample))]
        [SwaggerResponseExample(401, typeof(BadAuthExample))]
        [ProducesResponseType(typeof(ApiObjectResponse<UserDTO>), StatusCodes.Status200OK,           "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse),         StatusCodes.Status401Unauthorized, "application/json")]
        public IActionResult AboutMeUserInfo()
        {
            int userId  = Utils.GetUserIdFromClaims(User.Claims, out bool userAuthorized);
            var invoker = _userRepository.GetById(userId);
            if (!userAuthorized || invoker == null || invoker.IsBanned) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            var accountsBalancesSum = _accountRepository.GetAll().Where(acc => acc.UserId == invoker.Id).Sum(acc => acc.Balance);

            var userDTO = new UserDTO()
            {
                TelegramUserId      = invoker.TelegramUserId,
                FirstName           = invoker.FirstName,
                LastName            = invoker.LastName,
                BalanceUSD          = invoker.BalanceUSD,
                LanguageCode        = invoker.LanguageCode,
                OwnReferralCode     = invoker.OwnReferralCode,
                PhotoUrl            = invoker.PhotoUrl,
                AccountsBalancesSum = accountsBalancesSum
            };

            return Ok(new ApiObjectResponse<UserDTO>()
            {
                ok   = true,
                data = userDTO
            });
        }

        // GET: api/User/MyReferrals
        [HttpGet("MyReferrals")]
        [SwaggerResponse(200, "Success. The info about our user's referrals and earnings from them.")]
        [SwaggerResponse(401, "No auth from user.")]
        [SwaggerResponseExample(200, typeof(MyReferralsOkExample))]
        [SwaggerResponseExample(401, typeof(BadAuthExample))]
        [ProducesResponseType(typeof(ApiObjectResponse<List<ReferralsOutputModel>>), StatusCodes.Status200OK,   "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse),                      StatusCodes.Status401Unauthorized, "application/json")]
        public IActionResult MyReferrals()
        {
            int userId  = Utils.GetUserIdFromClaims(User.Claims, out bool userAuthorized);
            var invoker = _userRepository.GetById(userId);
            if (!userAuthorized || invoker == null || invoker.IsBanned) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            var ourReferralsIds = _referralRepository.GetAll().Where(r => r.HostUserId == invoker.Id).Select(r => r.DependentUserId);
            List<ReferralsOutputModel> referrals = new();
            foreach (var id in ourReferralsIds)
            {
                // TODO: get host earnings from the Transactions table

                var referral = _userRepository.GetById(id);
                if (referral != null) referrals.Add(new ReferralsOutputModel
                {
                    FirstName    = referral.FirstName,
                    LastName     = referral.LastName,
                    HostEarnings = 0,
                    PhotoUrl     = referral.PhotoUrl
                });
            }

            return Ok(new ApiObjectResponse<List<ReferralsOutputModel>>()
            {
                ok   = true,
                data = referrals
            });
        }

        // POST: /api/User/ChangeMyReferralCode
        [HttpPost("ChangeMyReferralCode")]
        [SwaggerResponse(200, "Success. User's referral code was updated to his own.")]
        [SwaggerResponse(400, "No such account that belongs to our user.")]
        [SwaggerResponse(401, "No auth from user.")]
        [SwaggerResponseExample(200, typeof(ChangeUsersReferralCodeOkExample))]
        [SwaggerResponseExample(400, typeof(ChangeUsersReferralCodeBadExample))]
        [SwaggerResponseExample(401, typeof(BadAuthExample))]
        [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status200OK,           "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest,   "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status401Unauthorized, "application/json")]
        public IActionResult ChangeUsersReferralCode([FromBody] ChangeReferralCodeInputModel model)
        {
            int userId  = Utils.GetUserIdFromClaims(User.Claims, out bool userAuthorized);
            var invoker = _userRepository.GetById(userId);
            if (!userAuthorized || invoker == null || invoker.IsBanned) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            Regex regex = new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d]{10}$");
            if (!regex.IsMatch(model.referralCode))
            {
                return BadRequest(new ApiMessageResponse
                {
                    ok      = false,
                    message = "Validation failed. Use 10 alphanumeric symbols.",
                });
            }

            invoker.OwnReferralCode = model.referralCode;
            _userRepository.Update(invoker);

            return Ok(new ApiMessageResponse
            {
                ok      = true,
                message = "Your own referral code was updated successfully."
            });
        }
    }
}
