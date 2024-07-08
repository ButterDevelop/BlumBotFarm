using AutoBlumFarmServer.ApiResponses;
using AutoBlumFarmServer.ApiResponses.AccountController;
using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace AutoBlumFarmServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountController : Controller
    {
        private readonly AccountRepository _accountRepository;
        private readonly UserRepository    _userRepository;

        public AccountController(AccountRepository accountRepository, UserRepository userRepository)
        {
            _accountRepository = accountRepository;
            _userRepository    = userRepository;
        }

        // GET: api/Account
        [HttpGet]
        [SwaggerResponse(200, "Success. If `blumAuthData` is empty - means that is a slot.")]
        [SwaggerResponse(401, "The only failure status code - no auth from user.")]
        [SwaggerResponseExample(200, typeof(GetAllAccountsOkExample))]
        [SwaggerResponseExample(401, typeof(BadAuthExample))]
        [ProducesResponseType(typeof(ApiObjectResponse<List<Account>>), StatusCodes.Status200OK,           "application/json")]
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

            var invokersAccounts = _accountRepository.GetAll().Where(acc => acc.UserId == userId).ToArray();

            return Json(invokersAccounts);
        }
        
        // GET: api/Account/5
        [HttpGet("{id}")]
        [SwaggerResponse(200, "Success. Once again: if `blumAuthData` is empty - means that is a slot.")]
        [SwaggerResponse(400, "No such account that belongs to our user.")]
        [SwaggerResponse(401, "No auth from user.")]
        [SwaggerResponseExample(200, typeof(GetAccountByIdOkExample))]
        [SwaggerResponseExample(400, typeof(GetAccountById400BadExample))]
        [SwaggerResponseExample(401, typeof(BadAuthExample))]
        [ProducesResponseType(typeof(ApiObjectResponse<Account>), StatusCodes.Status200OK,   "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status400BadRequest,   "application/json")]
        [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status401Unauthorized, "application/json")]
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
                message = "No such account."
            });

            return Json(account);
        }

        // POST: api/Account/CheckAccountUsername
        [HttpPost("CheckAccountUsername")]
        [ProducesResponseType(typeof(GetAllAccountsOkExample), StatusCodes.Status200OK,         "application/json")]
        [ProducesResponseType(typeof(BadAuthExample),          StatusCodes.Status400BadRequest, "application/json")]
        public IActionResult CheckAccountUsername([FromBody] string username)
        {
            int userId  = Utils.GetUserIdFromClaims(User.Claims, out bool userAuthorized);
            var invoker = _userRepository.GetById(userId);
            if (!userAuthorized || invoker == null || invoker.IsBanned) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            var account = _accountRepository.GetAll().FirstOrDefault(acc => acc.UserId == userId && acc.Username == username);
            if (account != null) return BadRequest();

            return Ok();
        }

        // POST: api/Account
        [HttpPost]
        [ProducesResponseType(typeof(GetAllAccountsOkExample), StatusCodes.Status200OK,         "application/json")]
        [ProducesResponseType(typeof(BadAuthExample),          StatusCodes.Status400BadRequest, "application/json")]
        public IActionResult CreateAccount([FromBody] Account account)
        {
            // Заглушка для создания нового аккаунта
            return Ok(new { Message = "Аккаунт создан", Account = account });
        }

        // PUT: api/Account/5
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(GetAllAccountsOkExample), StatusCodes.Status200OK,         "application/json")]
        [ProducesResponseType(typeof(BadAuthExample),          StatusCodes.Status400BadRequest, "application/json")]
        public IActionResult UpdateAccount(int id, [FromBody] string providerToken)
        {
            int userId  = Utils.GetUserIdFromClaims(User.Claims, out bool userAuthorized);
            var invoker = _userRepository.GetById(userId);
            if (!userAuthorized || invoker == null || invoker.IsBanned) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            var account = _accountRepository.GetById(id);
            if (account == null || account.UserId != userId) return BadRequest();

            account.ProviderToken = providerToken;
            _accountRepository.Update(account);

            // Заглушка для обновления аккаунта
            return Ok();
        }

        // DELETE: api/Account/5
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(GetAllAccountsOkExample), StatusCodes.Status200OK,         "application/json")]
        [ProducesResponseType(typeof(BadAuthExample),          StatusCodes.Status400BadRequest, "application/json")]
        public IActionResult DeactivateAccount(int id)
        {
            int userId  = Utils.GetUserIdFromClaims(User.Claims, out bool userAuthorized);
            var invoker = _userRepository.GetById(userId);
            if (!userAuthorized || invoker == null || invoker.IsBanned) return Unauthorized(new ApiMessageResponse
            {
                ok      = false,
                message = "No auth."
            });

            var account = _accountRepository.GetById(id);
            if (account == null || account.UserId != userId) return BadRequest();

           // ????

            return Ok();
        }
    }
}
