using AutoBlumFarmServer.DTO;
using Swashbuckle.AspNetCore.Filters;

namespace AutoBlumFarmServer.ApiResponses.AccountController
{
    public class GetAccountByIdOkExample : IMultipleExamplesProvider<ApiObjectResponse<AccountDTO>>
    {
        public IEnumerable<SwaggerExample<ApiObjectResponse<AccountDTO>>> GetExamples()
        {
            yield return SwaggerExample.Create("The account data", new ApiObjectResponse<AccountDTO>()
            {
                ok   = true,
                data = new AccountDTO() { Id = 1, Balance = 1000, BlumAuthData = "authToken", Tickets = 3, Username = "UserName" }
            });
            yield return SwaggerExample.Create("Empty slot", new ApiObjectResponse<AccountDTO>()
            {
                ok   = true,
                data = new AccountDTO() { Id = 1, Balance = 0, BlumAuthData = "", Tickets = 0, Username = "" }
            });
        }
    }

    public class GetAccountById400BadExample : IMultipleExamplesProvider<ApiMessageResponse>
    {
        public IEnumerable<SwaggerExample<ApiMessageResponse>> GetExamples()
        {
            yield return SwaggerExample.Create("No such account", new ApiMessageResponse()
            {
                ok      = true,
                message = "No such account that belongs to our user."
            });
        }
    }
}
