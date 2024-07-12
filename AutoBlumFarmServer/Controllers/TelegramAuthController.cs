using AutoBlumFarmServer.ApiResponses;
using AutoBlumFarmServer.ApiResponses.TelegramAuthController;
using AutoBlumFarmServer.Model;
using BlumBotFarm.Core.Models;
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
        private const int REFERRAL_CODE_STRING_LENGTH = 10;

        private readonly UserRepository     _userRepository;
        private readonly ReferralRepository _referralRepository;

        public TelegramAuthController(UserRepository userRepository, ReferralRepository referralRepository)
        {
            _userRepository     = userRepository;
            _referralRepository = referralRepository;
        }

        // POST: api/TelegramAuth
        [HttpPost]
        [SwaggerResponse(200, "Success. Token returned.")]
        [SwaggerResponse(401, "Unauthorized.")]
        [SwaggerResponseExample(200, typeof(TelegramAuthOkExample))]
        [SwaggerResponseExample(401, typeof(TelegramAuthBadExample))]
        [ProducesResponseType(typeof(ApiObjectResponse<string>), StatusCodes.Status200OK,           "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse),        StatusCodes.Status401Unauthorized, "application/json")]
        public IActionResult Authenticate([FromBody] TelegramAuthModel model)
        {
            string query = model.query;

            var data = HttpUtility.ParseQueryString(query);

            if (ValidateTelegramData(query, Config.Instance.JWT_KEY, out SortedDictionary<string, string> parameters))
            {
                if (!parameters.ContainsKey("user"))
                {
                    long userTelegramId = -1;
                    string languageCode = "en", firstName = "", lastName = "", photoUrl = "";
                    try
                    {
                        dynamic json   = JObject.Parse(parameters["user"].Replace("'", "\\'").Replace("\"", "'"));
                        userTelegramId = json.id;
                        languageCode   = json.language_code;
                        firstName      = json.first_name;
                        lastName       = json.last_name;
                        photoUrl       = json.photo_url;
                    }
                    catch { }

                    if (userTelegramId > 0)
                    {
                        var token = GenerateJwtToken(userTelegramId, languageCode, firstName, lastName, photoUrl, model.referralCode);

                        if (token != null)
                        {
                            return Ok(new ApiMessageResponse()
                            {
                                ok = false,
                                message = token
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
        private bool ValidateTelegramData(string initData, string botToken, out SortedDictionary<string, string> parameters)
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

        private string? GenerateJwtToken(long telegramUserId, string languageCode, string firstName, string lastName, 
                                         string photoUrl, string? hostReferralCode)
        {
            var users = _userRepository.GetAll();

            var user = users.FirstOrDefault(acc => acc.TelegramUserId == telegramUserId);
            if (user == null)
            {
                string usersReferralCode;
                do
                {
                    usersReferralCode = Utils.RandomString(REFERRAL_CODE_STRING_LENGTH);
                } while (users.FirstOrDefault(u => u.OwnReferralCode == usersReferralCode) != null);

                user = new User()
                {
                    BalanceUSD      = 0M,
                    TelegramUserId  = telegramUserId,
                    FirstName       = firstName,
                    LastName        = lastName,
                    IsBanned        = false,
                    LanguageCode    = languageCode,
                    OwnReferralCode = usersReferralCode,
                    CreatedAt       = DateTime.Now,
                    PhotoUrl        = photoUrl
                };
                _userRepository.Add(user);

                user = _userRepository.GetAll().FirstOrDefault(acc => acc.TelegramUserId == telegramUserId);
                if (user == null) return null;

                if (hostReferralCode != null)
                {
                    var hostUser = users.FirstOrDefault(u => u.OwnReferralCode == hostReferralCode);
                    if (hostUser != null)
                    {
                        var referral = new Referral
                        {
                            HostUserId      = hostUser.Id,
                            DependentUserId = user.Id
                        };
                        _referralRepository.Add(referral);
                    }
                }
            }

            user.FirstName    = firstName;
            user.LastName     = lastName;
            user.LanguageCode = languageCode;
            user.PhotoUrl     = photoUrl;
            _userRepository.Update(user);

            var claims = new List<Claim>
            {
                new("Id", user.Id.ToString())
            };

            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Config.Instance.JWT_KEY));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(Config.Instance.JWT_ISSUER,
                                             Config.Instance.JWT_AUDIENCE,
                                             claims, 
                                             expires: DateTime.Now.AddHours(8),
                                             signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
