using AutoBlumFarmServer.ApiResponses;
using AutoBlumFarmServer.ApiResponses.AccountController;
using AutoBlumFarmServer.ApiResponses.UserController;
using AutoBlumFarmServer.DTO;
using AutoBlumFarmServer.Model;
using BlumBotFarm.Database.Repositories;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using System.Text.RegularExpressions;

namespace AutoBlumFarmServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        private readonly UserRepository _userRepository;

        public UserController(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // GET: api/User
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

            var userDTO = new UserDTO()
            {
                TelegramUserId  = invoker.TelegramUserId,
                BalanceUSD      = invoker.BalanceUSD,
                LanguageCode    = invoker.LanguageCode,
                OwnReferralCode = invoker.OwnReferralCode,
            };

            return Ok(new ApiObjectResponse<UserDTO>()
            {
                ok   = true,
                data = userDTO
            });
        }

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
        public IActionResult ChangeUsersReferralCode([FromBody] ChangeReferralCodeModel model)
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
