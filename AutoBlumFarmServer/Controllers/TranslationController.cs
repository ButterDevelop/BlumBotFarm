using AutoBlumFarmServer.ApiResponses;
using AutoBlumFarmServer.ApiResponses.UserController;
using AutoBlumFarmServer.Helpers;
using BlumBotFarm.Database.Repositories;
using BlumBotFarm.Translation;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace AutoBlumFarmServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TranslationController : Controller
    {
        private readonly UserRepository _userRepository;

        public TranslationController(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // GET: api/Translation/ru
        [HttpGet("{lang}")]
        [SwaggerResponse(200, "Success.")]
        [SwaggerResponse(401, "No auth from user.")]
        [SwaggerResponse(404, "Translation was not found.")]
        [SwaggerResponseExample(200, typeof(TranslationOkExample))]
        [SwaggerResponseExample(401, typeof(BadAuthExample))]
        [SwaggerResponseExample(404, typeof(TranslationBadExample))]
        [ProducesResponseType(typeof(ApiObjectResponse<List<TranslationModel>>), StatusCodes.Status200OK,           "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse),                        StatusCodes.Status401Unauthorized, "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse),                        StatusCodes.Status404NotFound,     "application/json")]
        public async Task<IActionResult> GetTranslations(string lang)
        {
            int userId  = Utils.GetUserIdFromClaims(User.Claims, out bool userAuthorized);
            var invoker = _userRepository.GetById(userId);
            if (!userAuthorized || invoker == null || invoker.IsBanned) return Unauthorized(new ApiMessageResponse
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
    }
}
