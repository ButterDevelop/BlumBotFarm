using AutoBlumFarmServer.ApiResponses;
using AutoBlumFarmServer.ApiResponses.TelegramAuthController;
using AutoBlumFarmServer.Helpers;
using AutoBlumFarmServer.Model;
using BlumBotFarm.Database.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace AutoBlumFarmServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TelegramAuthController : Controller
    {
        private readonly UserRepository     _userRepository;
        private readonly ReferralRepository _referralRepository;

        public TelegramAuthController(UserRepository userRepository, ReferralRepository referralRepository)
        {
            _userRepository     = userRepository;
            _referralRepository = referralRepository;
        }

        // POST: api/TelegramAuth
        [HttpPost]
        [SwaggerResponse(200, "Success. Token returned, and date it expires (UTC format), and also Language code.")]
        [SwaggerResponse(401, "Unauthorized.")]
        [SwaggerResponseExample(200, typeof(TelegramAuthOkExample))]
        [SwaggerResponseExample(401, typeof(TelegramAuthBadExample))]
        [ProducesResponseType(typeof(ApiObjectResponse<TGAuthOutputModel>), StatusCodes.Status200OK,           "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse),                   StatusCodes.Status401Unauthorized, "application/json")]
        public IActionResult Authenticate([FromBody] TGAuthInputModel model)
        {
            if (string.IsNullOrEmpty(model.query))
            {
                return Unauthorized(new ApiMessageResponse()
                {
                    ok      = false,
                    message = "No data provided."
                });
            }

            var query = HttpUtility.UrlDecode(model.query);

            if (ValidateTelegramData(query, Config.Instance.TELEGRAM_BOT_TOKEN, out SortedDictionary<string, string> parameters))
            {
                if (parameters.ContainsKey("user"))
                {
                    long userTelegramId = -1;
                    string languageCode = "en", firstName = "", lastName = "";
                    try
                    {
                        dynamic json   = JObject.Parse(parameters["user"].Replace("'", "\\'").Replace("\"", "'"));
                        userTelegramId = json.id;
                        languageCode   = json.language_code;
                        firstName      = json.first_name;
                        lastName       = json.last_name;
                    }
                    catch { }

                    if (userTelegramId > 0)
                    {
                        (string? token, DateTime expires) = GenerateJwtToken(userTelegramId, languageCode, firstName, lastName);

                        if (token != null)
                        {
                            return Ok(new ApiObjectResponse<TGAuthOutputModel>()
                            {
                                ok   = true,
                                data = new()
                                {
                                    token        = token,
                                    expires      = expires,
                                    languageCode = languageCode
                                }
                            });
                        }
                    }
                }
            }
            
            return Unauthorized(new ApiMessageResponse() 
            {
                ok      = false,
                message = "Verification failed."
            });
        }

        // Метод для валидации данных от Telegram
        private static bool ValidateTelegramData(string initData, string botToken, out SortedDictionary<string, string> parameters)
        {
            // Парсинг строки initData от Telegram
            var data = HttpUtility.ParseQueryString(initData);
            
            // Помещение данных в отсортированный по алфавиту словарь
            var dataDict = new SortedDictionary<string, string>(
                data.AllKeys.ToDictionary(x => x!, x => data[x]!),
                StringComparer.Ordinal);
            
            // Константный ключ для генерации секретного ключа
            const string constantKey = "WebAppData";

            // Создание строки для проверки данных (data-check-string)
            var dataCheckString = string.Join(
                '\n', dataDict.Where(x => x.Key != "hash") // Удаляем hash из строки
                    .Select(x => $"{x.Key}={x.Value}"));
            
            // Генерация секретного ключа с использованием HMAC-SHA-256
            var secretKey = HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(constantKey), // WebAppData
                Encoding.UTF8.GetBytes(botToken)); // Токен бота
            
            // Генерация хэша данных
            var generatedHash = HMACSHA256.HashData(
                secretKey,
                Encoding.UTF8.GetBytes(dataCheckString)); // data-check-string
            
            // Преобразование полученного хэша от Telegram в массив байтов
            var actualHash = Convert.FromHexString(dataDict["hash"]); // .NET 5.0 и выше
            
            parameters = dataDict;
            
            // Сравнение нашего хэша с хэшем от Telegram
            return actualHash.SequenceEqual(generatedHash);
        }

        private (string? token, DateTime expired) GenerateJwtToken(long telegramUserId, string languageCode, string firstName, string lastName)
        {
            var users = _userRepository.GetAll();

            var user = users.FirstOrDefault(acc => acc.TelegramUserId == telegramUserId);
            if (user == null) return (null, DateTime.UtcNow);

            user.FirstName    = firstName;
            user.LastName     = lastName;
            user.LanguageCode = languageCode;
            _userRepository.Update(user);

            var claims = new List<Claim>
            {
                new("Id", user.Id.ToString())
            };

            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Config.Instance.JWT_KEY));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            DateTime expires = DateTime.UtcNow.AddHours(Config.Instance.JWT_LIVE_HOURS);

            var token = new JwtSecurityToken(Config.Instance.JWT_ISSUER,
                                             Config.Instance.JWT_AUDIENCE,
                                             claims, 
                                             expires: expires,
                                             signingCredentials: creds);

            return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        }
    }
}
