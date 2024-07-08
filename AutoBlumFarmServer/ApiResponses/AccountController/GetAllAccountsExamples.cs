using AutoBlumFarmServer.DTO;
using Swashbuckle.AspNetCore.Filters;

namespace AutoBlumFarmServer.ApiResponses.AccountController
{
    public class GetAllAccountsOkExample : IMultipleExamplesProvider<ApiObjectResponse<List<AccountDTO>>>
    {
        public IEnumerable<SwaggerExample<ApiObjectResponse<List<AccountDTO>>>> GetExamples()
        {
            yield return SwaggerExample.Create("1 account + 1 empty slot", new ApiObjectResponse<List<AccountDTO>>()
            {
                ok = true,
                data =
                [
                    new AccountDTO() { Id = 1, Balance = 1000, BlumAuthData = "authToken", Tickets = 3, Username = "UserName" },
                    new AccountDTO() { Id = 2, Balance = 0,    BlumAuthData = "",          Tickets = 0, Username = "" },
                ]
            });
            yield return SwaggerExample.Create("Empty account list", new ApiObjectResponse<List<AccountDTO>>()
            {
                ok = true,
                data = []
            });
        }
    }
}
