using AutoBlumFarmServer.SwaggerApiResponses;
using AutoBlumFarmServer.SwaggerApiResponses.AccountController;
using AutoBlumFarmServer.DTO;
using AutoBlumFarmServer.Helpers;
using AutoBlumFarmServer.Model;
using BlumBotFarm.Database.Repositories;
using BlumBotFarm.Translation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using System.Text.RegularExpressions;
using System.Web;
using BlumBotFarm.CacheUpdater.CacheServices;
using BlumBotFarm.Core.Models;

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
        private readonly TaskRepository        _taskRepository;
        private readonly ProxySellerAPIHelper  _proxySellerAPIHelper;
        private readonly IUserCacheService     _userCacheService;

        public AccountController(AccountRepository     accountRepository,     UserRepository       userRepository, 
                                 DailyRewardRepository dailyRewardRepository, EarningRepository    earningRepository,
                                 TaskRepository        taskRepository,        ProxySellerAPIHelper proxySellerAPIHelper,
                                 IUserCacheService     userCacheService)
        {
            _accountRepository     = accountRepository;
            _userRepository        = userRepository;
            _dailyRewardRepository = dailyRewardRepository;
            _earningRepository     = earningRepository;
            _taskRepository        = taskRepository;
            _proxySellerAPIHelper  = proxySellerAPIHelper;
            _userCacheService      = userCacheService;
        }

        private bool ValidateUsername(string username)
        {
            Regex regex = new(@"^[A-Za-z\d]{6,10}$");
            return regex.IsMatch(username);
        }

        // GET: api/Account
        [HttpGet]
        [SwaggerResponse(200, "Success. If `blumAuthData` is empty - means that is a slot.")]
        [SwaggerResponse(401, "The only failure status code - no auth from user.")]
        [SwaggerResponseExample(200, typeof(GetAllAccountsOkExample))]
        [SwaggerResponseExample(401, typeof(BadAuthExample))]
        [ProducesResponseType(typeof(ApiObjectResponse<List<AccountDTO>>), StatusCodes.Status200OK,           "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse),                  StatusCodes.Status401Unauthorized, "application/json")]
        public IActionResult GetAllAccounts()
        {
            int userId  = Utils.GetUserIdFromClaims(User.Claims, out bool userAuthorized);
            if (!userAuthorized || !_userCacheService.TryGetFromCache(userId, out User invoker)) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            var invokersAccounts = _accountRepository.GetAllFit(acc => acc.UserId == userId);

            var  today = DateTime.Now.Date; // Not UTC, here we are not using it because of Quartz in the main project
            var  dailyRewardsToday = _dailyRewardRepository.GetAllFit(r => r.CreatedAt > today);
            var  earningsToday     = _earningRepository.GetAllFit(earning => earning.Created > today);
            var  tasks             = _taskRepository.GetAll();

            List<AccountDTO> results = [];
            foreach (var account in invokersAccounts)
            {
                bool tookDailyRewardToday = dailyRewardsToday.Any(r => r.AccountId == account.Id);
                var  todayEarningsSum     = earningsToday.Where(earning => earning.AccountId == account.Id).Select(earning => earning.Total).DefaultIfEmpty(0).Sum();

                var dailyCheckJob = tasks.FirstOrDefault(task => task.AccountId == account.Id && task.TaskType == "DailyCheckJob");
                string nearestWorkIn = "-";
                if (dailyCheckJob != null && !string.IsNullOrEmpty(account.ProviderToken) && !string.IsNullOrEmpty(account.RefreshToken))
                {
                    var    dailyIn    = dailyCheckJob.NextRunTime - DateTime.Now;
                    string dailyMinus = dailyIn < TimeSpan.Zero ? "-" : "";
                    nearestWorkIn = $"{dailyMinus}{dailyIn:hh\\:mm\\:ss}";
                }

                results.Add(new AccountDTO
                {
                    Id              = account.Id,
                    CustomUsername  = account.CustomUsername,
                    BlumUsername    = account.BlumUsername,
                    Balance         = account.Balance,
                    Tickets         = account.Tickets,
                    ReferralCount   = account.ReferralsCount,
                    ReferralLink    = account.ReferralLink,
                    BlumAuthData    = account.ProviderToken,
                    EarnedToday     = todayEarningsSum,
                    TookDailyReward = tookDailyRewardToday,
                    NearestWorkIn   = nearestWorkIn,
                    CountryCode     = account.CountryCode,
                    LastStatus      = account.LastStatus,
                    IsTrial         = account.IsTrial,
                    TrialExpires    = account.TrialExpires
                });
            }

            return Ok(new ApiObjectResponse<List<AccountDTO>>
            {
                ok   = true,
                data = results.OrderBy(r => r.Id).ToList()
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
            if (!userAuthorized || !_userCacheService.TryGetFromCache(userId, out User invoker)) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            var account = _accountRepository.GetById(id);
            if (account == null || account.UserId != userId) return BadRequest(new ApiMessageResponse
            {
                ok      = false,
                message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_NO_SUCH_ACCOUNT%#")
            });

            var  today = DateTime.Now.Date; // Not UTC, here we are not using it because of Quartz in the main project

            bool tookDailyRewardToday = _dailyRewardRepository.GetAllFit(r => r.CreatedAt > today && r.AccountId == account.Id).Count() > 0;
            var  todayEarningsSum     = _earningRepository
                                            .GetAll()
                                            .Where(earning => earning.Created > today && earning.AccountId == account.Id)
                                            .Select(earning => earning.Total)
                                            .DefaultIfEmpty(0).Sum();

            var dailyCheckJob    = _taskRepository.GetAllFit(task => task.AccountId == account.Id && task.TaskType == "DailyCheckJob").FirstOrDefault();
            string nearestWorkIn = "-";
            if (dailyCheckJob != null && !string.IsNullOrEmpty(account.ProviderToken) && !string.IsNullOrEmpty(account.RefreshToken))
            {
                var    dailyIn    = dailyCheckJob.NextRunTime - DateTime.Now;
                string dailyMinus = dailyIn < TimeSpan.Zero ? "-" : "";
                nearestWorkIn = $"{dailyMinus}{dailyIn:hh\\:mm\\:ss}";
            }

            return Ok(new ApiObjectResponse<AccountDTO>
            { 
                ok   = true,
                data = new()
                {
                    Id              = account.Id,
                    CustomUsername  = account.CustomUsername,
                    BlumUsername    = account.BlumUsername,
                    Balance         = account.Balance,
                    Tickets         = account.Tickets,
                    ReferralCount   = account.ReferralsCount,
                    ReferralLink    = account.ReferralLink,
                    BlumAuthData    = account.ProviderToken,
                    EarnedToday     = todayEarningsSum,
                    TookDailyReward = tookDailyRewardToday,
                    NearestWorkIn   = nearestWorkIn,
                    CountryCode     = account.CountryCode,
                    LastStatus      = account.LastStatus,
                    IsTrial         = account.IsTrial,
                    TrialExpires    = account.TrialExpires
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
            if (!userAuthorized || !_userCacheService.TryGetFromCache(userId, out User invoker)) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            if (!ValidateUsername(username))
            {
                return BadRequest(new ApiMessageResponse
                {
                    ok      = false,
                    message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_USERNAME_VALIDATION_FAILED%#")
                });
            }

            var account = _accountRepository.GetAllFit(acc => acc.CustomUsername == username).FirstOrDefault();
            if (account != null) return BadRequest(new ApiMessageResponse
            {
                ok      = false,
                message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_USERNAME_IS_OCCUPIED%#")
            });

            return Ok(new ApiMessageResponse
            {
                ok      = true,
                message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_USERNAME_IS_AVAILABLE%#")
            });
        }

        // GET: api/Account/AllGeo
        [HttpGet("AllGeo")]
        [SwaggerResponse(200, "Success. All geo returned.")]
        [SwaggerResponse(401, "No auth from user.")]
        [SwaggerResponseExample(200, typeof(AllGeoOkExample))]
        [SwaggerResponseExample(401, typeof(BadAuthExample))]
        [ProducesResponseType(typeof(ApiObjectResponse<AllGeoOutputModel>), StatusCodes.Status200OK,           "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse),                   StatusCodes.Status401Unauthorized, "application/json")]
        public IActionResult AllGeo()
        {
            int userId  = Utils.GetUserIdFromClaims(User.Claims, out bool userAuthorized);
            if (!userAuthorized || !_userCacheService.TryGetFromCache(userId, out User invoker)) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            AllGeoOutputModel output = new()
            {
                Geos = []
            };
            foreach (var item in Config.Instance.GEO_PROXY_SELLER)
            {
                output.Geos.Add(new()
                {
                    CountryCode    = item.Key,
                    CountryName    = item.Value.countryName,
                    TimezoneOffset = item.Value.timezoneOffset
                });
            }

            return Ok(new ApiObjectResponse<AllGeoOutputModel>
            {
                ok   = true,
                data = output
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
        public IActionResult UpdateAccount(int id, [FromBody] UpdateAccountInputModel model)
        {
            int userId  = Utils.GetUserIdFromClaims(User.Claims, out bool userAuthorized);
            if (!userAuthorized || !_userCacheService.TryGetFromCache(userId, out User invoker)) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            var account = _accountRepository.GetById(id);
            if (account == null || account.UserId != userId) return BadRequest(new ApiMessageResponse
            {
                ok      = false,
                message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_NO_SUCH_ACCOUNT%#")
            });

            if (account.CustomUsername != model.CustomUsername)
            {
                if (!ValidateUsername(model.CustomUsername))
                {
                    return BadRequest(new ApiMessageResponse
                    {
                        ok      = false,
                        message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_CUSTOM_USERNAME_VALIDATION_FAILED%#")
                    });
                }

                var accountCheckUsername = _accountRepository.GetAllFit(acc => acc.CustomUsername == model.CustomUsername && acc.Id != account.Id).FirstOrDefault();
                if (accountCheckUsername != null) return BadRequest(new ApiMessageResponse
                {
                    ok      = false,
                    message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_USERNAME_IS_OCCUPIED%#")
                });
            }

            if (account.ProviderToken != model.BlumTelegramAuth)
            {
                if (model.BlumTelegramAuth.Contains("/#tgWebAppData="))
                {
                    Regex regex = new("/#tgWebAppData=(.*)&tg");
                    if (regex.IsMatch(model.BlumTelegramAuth))
                    {
                        model.BlumTelegramAuth = HttpUtility.UrlDecode(regex.Match(model.BlumTelegramAuth).Groups[1].Value);
                    }
                }

                if (!model.BlumTelegramAuth.Contains("auth_date") ||
                    !model.BlumTelegramAuth.Contains("user") ||
                    !model.BlumTelegramAuth.Contains("hash"))
                {
                    return BadRequest(new ApiMessageResponse
                    {
                        ok      = false,
                        message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_BLUM_TELEGRAM_AUTH_VALIDATION_FAILED%#")
                    });
                }
            }

            if (account.CountryCode != model.CountryCode)
            {
                if (!Config.Instance.GEO_PROXY_SELLER.ContainsKey(model.CountryCode))
                {
                    return BadRequest(new ApiMessageResponse
                    {
                        ok      = false,
                        message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_COUNTRY_CODE_VALIDATION_FAILED%#")
                    });
                }

                if (account.ProxySellerListId != 0) _ = _proxySellerAPIHelper.DeleteResident(invoker.Id, account.Id, account.ProxySellerListId);
                
                var (addProxyResult, listId) = _proxySellerAPIHelper.AddResident(invoker.Id, account.Id, model.CountryCode);
                if (!addProxyResult)
                {
                    return BadRequest(new ApiMessageResponse
                    {
                        ok      = false,
                        message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_CANT_ADD_PROXY_TO_PROXY_SERVICE%#")
                    });
                }

                var (downloadFileResult, content) = _proxySellerAPIHelper.DownloadFile(invoker.Id, account.Id, listId);
                if (!downloadFileResult)
                {
                    return BadRequest(new ApiMessageResponse
                    {
                        ok      = false,
                        message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_CANT_GET_PROXY_FROM_PROXY_SERVICE%#")
                    });
                }

                if (content.Contains('@'))
                {
                    content = content[(content.IndexOf('@') + 1)..] + "@" + content[..(content.IndexOf('@') - 1)];
                }
                else
                {
                    return BadRequest(new ApiMessageResponse
                    {
                        ok      = false,
                        message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_PROXY_SERVICE_RETURNED_WRONG_FORMAT%#")
                    });
                }

                account.Proxy       = "http://" + content;
                account.CountryCode = model.CountryCode;
            }

            account.CustomUsername = model.CustomUsername;
            account.ProviderToken  = model.BlumTelegramAuth;
            if (string.IsNullOrEmpty(account.CustomUsername) && string.IsNullOrEmpty(account.ProviderToken))
            {
                // If this account is an empty slot, than telling the user about we took his account
                account.LastStatus = "#%JOB_LAST_STATUS_IN_THE_QUEUE%#";
            }
            _accountRepository.Update(account);

            return Ok(new ApiMessageResponse
            {
                ok      = true,
                message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_ACCOUNT_WAS_UPDATED_SUCCESSFULLY%#")
            });
        }
    }
}
