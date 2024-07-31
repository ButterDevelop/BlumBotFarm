using AutoBlumFarmServer.SwaggerApiResponses;
using AutoBlumFarmServer.SwaggerApiResponses.UserController;
using AutoBlumFarmServer.Helpers;
using BlumBotFarm.Database.Repositories;
using BlumBotFarm.Translation;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using AutoBlumFarmServer.Model;
using BlumBotFarm.CacheUpdater.CacheServices;
using BlumBotFarm.Core.Models;
using Microsoft.AspNetCore.Authorization;

namespace AutoBlumFarmServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TranslationController : Controller
    {
        private readonly UserRepository    _userRepository;
        private readonly IUserCacheService _userCacheService;

        public TranslationController(UserRepository userRepository, IUserCacheService userCacheService)
        {
            _userRepository   = userRepository;
            _userCacheService = userCacheService;
        }

        // GET: api/Translation/ru
        [HttpGet("{lang}")]
        [SwaggerResponse(200, "Success.")]
        [SwaggerResponse(401, "No auth from user.")]
        [SwaggerResponse(404, "Translation was not found.")]
        [SwaggerResponseExample(200, typeof(GetTranslationsOkExample))]
        [SwaggerResponseExample(401, typeof(BadAuthExample))]
        [SwaggerResponseExample(404, typeof(GetTranslationsBadExample))]
        [ProducesResponseType(typeof(ApiObjectResponse<List<TranslationModel>>), StatusCodes.Status200OK,           "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse),                        StatusCodes.Status401Unauthorized, "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse),                        StatusCodes.Status404NotFound,     "application/json")]
        public async Task<IActionResult> GetTranslations(string lang)
        {
            int userId  = Utils.GetUserIdFromClaims(User.Claims, out bool userAuthorized);
            if (!userAuthorized || !_userCacheService.TryGetFromCache(userId, out User invoker)) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            var filePath = Path.Combine("Translations", $"{lang}.json");
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new ApiMessageResponse
                {
                    ok      = false,
                    message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_TRANSLATION_NOT_FOUND%#")
                });
            }

            var json = await System.IO.File.ReadAllTextAsync(filePath);
            var obj  = JsonConvert.DeserializeObject<List<TranslationModel>>(json);
            return Ok(new ApiObjectResponse<List<TranslationModel>>
            {
                ok   = true,
                data = obj
            });
        }

        // GET: api/Translation/GetAvailableLanguages
        [HttpGet("GetAvailableLanguages")]
        [SwaggerResponse(200, "Success.")]
        [SwaggerResponse(401, "No auth from user.")]
        [SwaggerResponseExample(200, typeof(GetAvailableLanguagesOkExample))]
        [SwaggerResponseExample(401, typeof(BadAuthExample))]
        [ProducesResponseType(typeof(ApiObjectResponse<Dictionary<string, string>>), StatusCodes.Status200OK,           "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse),                            StatusCodes.Status401Unauthorized, "application/json")]
        public IActionResult GetAvailableLanguages()
        {
            int userId  = Utils.GetUserIdFromClaims(User.Claims, out bool userAuthorized);
            if (!userAuthorized || !_userCacheService.TryGetFromCache(userId, out User invoker)) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            Dictionary<string, string> dict = [];
            foreach (var key in TranslationHelper.Instance.AvailableLanguageCodes)
            {
                if (TranslationHelper.LanguageCodeToLanguageName.TryGetValue(key, out string? value)) dict.Add(key, value);
            }

            return Ok(new ApiObjectResponse<Dictionary<string, string>>
            {
                ok   = true,
                data = dict
            });
        }

        // POST: api/Translation/ChangeLanguage
        [HttpPost("ChangeLanguage")]
        [SwaggerResponse(200, "Success.")]
        [SwaggerResponse(401, "No auth from user.")]
        [SwaggerResponse(404, "No such language.")]
        [SwaggerResponseExample(200, typeof(ChangeLanguageOkExample))]
        [SwaggerResponseExample(401, typeof(BadAuthExample))]
        [SwaggerResponseExample(404, typeof(ChangeLanguageBadExample))]
        [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status200OK,           "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status401Unauthorized, "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status404NotFound,     "application/json")]
        public IActionResult ChangeLanguage(ChangeLanguageInputModel model)
        {
            int userId  = Utils.GetUserIdFromClaims(User.Claims, out bool userAuthorized);
            if (!userAuthorized || !_userCacheService.TryGetFromCache(userId, out User invoker)) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            if (!string.IsNullOrEmpty(model.LanguageCode) && 
                !TranslationHelper.Instance.AvailableLanguageCodes.Contains(model.LanguageCode.ToLower()))
            {
                return NotFound(new ApiMessageResponse
                {
                    ok      = false,
                    message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_LANGUAGE_NOT_FOUND%#")
                });
            }

            // Update user in DB
            invoker.LanguageCode = model.LanguageCode.ToLower();
            _userRepository.Update(invoker);

            // Update changes to cache
            _userCacheService.SetInCache(invoker);

            return Ok(new ApiMessageResponse
            {
                ok      = true,
                message = TranslationHelper.Instance.Translate(invoker.LanguageCode, "#%MESSAGE_LANGUAGE_WAS_CHANGED_SUCCESSFULLY%#")
            });
        }
    }
}
