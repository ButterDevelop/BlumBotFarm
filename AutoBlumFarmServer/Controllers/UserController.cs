using AutoBlumFarmServer.SwaggerApiResponses;
using AutoBlumFarmServer.SwaggerApiResponses.UserController;
using AutoBlumFarmServer.DTO;
using AutoBlumFarmServer.Helpers;
using AutoBlumFarmServer.Model;
using AutoBlumFarmServer.Properties;
using BlumBotFarm.Database.Repositories;
using BlumBotFarm.Translation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using System.Text.RegularExpressions;
using Telegram.Bot;

namespace AutoBlumFarmServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : Controller
    {
        private readonly UserRepository         _userRepository;
        private readonly AccountRepository      _accountRepository;
        private readonly ReferralRepository     _referralRepository;
        private readonly StarsPaymentRepository _starsPaymentRepository;
        private readonly ITelegramBotClient     _botClient;

        public UserController(UserRepository userRepository, AccountRepository accountRepository, ReferralRepository referralRepository,
                              StarsPaymentRepository starsPaymentRepository, TelegramBotClient botClient)
        {
            _userRepository         = userRepository;
            _accountRepository      = accountRepository;
            _referralRepository     = referralRepository;
            _starsPaymentRepository = starsPaymentRepository;
            _botClient              = botClient;
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

            var accountsBalancesSum = _accountRepository.GetAll()
                                                        .Where(acc => acc.UserId == invoker.Id)
                                                        .Select(acc => acc.Balance)
                                                        .DefaultIfEmpty(0).Sum();

            var userDTO = new UserDTO()
            {
                TelegramUserId      = invoker.TelegramUserId,
                FirstName           = invoker.FirstName,
                LastName            = invoker.LastName,
                BalanceUSD          = invoker.BalanceUSD,
                LanguageCode        = invoker.LanguageCode,
                OwnReferralCode     = invoker.OwnReferralCode,
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
        [ProducesResponseType(typeof(ApiObjectResponse<List<ReferralsOutputModel>>), StatusCodes.Status200OK,           "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse),                            StatusCodes.Status401Unauthorized, "application/json")]
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
            List<ReferralsOutputModel> referrals = [];
            foreach (var id in ourReferralsIds)
            {
                var referral = _userRepository.GetById(id);
                if (referral != null)
                {
                    decimal hostEarningsUsd = _starsPaymentRepository.GetAll()
                                                                     .Where(p => p.UserId == id && p.IsCompleted)
                                                                     .Select(p => p.AmountUsd)
                                                                     .DefaultIfEmpty(0).Sum();

                    referrals.Add(new ReferralsOutputModel
                    {
                        Id           = referral.Id,
                        FirstName    = referral.FirstName,
                        LastName     = referral.LastName,
                        HostEarnings = hostEarningsUsd
                    });
                }
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
                    message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_REFERRAL_CODE_VALIDATION_FAILED%#")
                });
            }

            invoker.OwnReferralCode = model.referralCode;
            _userRepository.Update(invoker);

            return Ok(new ApiMessageResponse
            {
                ok      = true,
                message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_YOUR_OWN_REFERRAL_CODE_WAS_UPDATED%#")
            });
        }

        // GET: /api/User/GetUserAvatar/5
        [HttpGet("GetUserAvatar/{userId}")]
        [AllowAnonymous]
        [SwaggerResponse(200, "Success. User's avatar or placeholder image returned.")]
        [SwaggerResponse(400, "User not found.")]
        [ProducesResponseType(typeof(MemoryStream), StatusCodes.Status200OK, "image/png")]
        public async Task<IActionResult> GetUserAvatar(int userId)
        {
            var user = _userRepository.GetById(userId);
            if (user == null) return NotFound();

            var usersPhoto = await _botClient.GetUserProfilePhotosAsync(user.TelegramUserId);
            if (usersPhoto.TotalCount > 0)
            {
                var fileId = usersPhoto.Photos[0][0].FileId;
                var file   = await _botClient.GetFileAsync(fileId);

                var url    = $"https://api.telegram.org/file/bot{Config.Instance.TELEGRAM_BOT_TOKEN}/{file.FilePath}";
                using (var httpClient = new HttpClient())
                {
                    var photoBytes = await httpClient.GetByteArrayAsync(url);
                    var stream     = new MemoryStream(photoBytes);

                    // Установка заголовков для кэширования
                    Response.Headers.Append("Cache-Control", "public, max-age=86400"); // 86400 seconds = 24 hours
                    return File(stream, "image/png");
                }
            }

            var byteArray = Resources.defaultAvatar;
            MemoryStream memoryStream = new(byteArray);
            return File(memoryStream, "image/png"); // Если аватарка не найдена
        }
    }
}
