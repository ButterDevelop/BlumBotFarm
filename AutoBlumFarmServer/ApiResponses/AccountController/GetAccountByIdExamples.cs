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
                data = new()
                {
                    Username        = "UserName",
                    Balance         = 40193.7,
                    Tickets         = 4,
                    ReferralCount   = 2,
                    ReferralLink    = "t.me/BlumCryptoBot/app?startapp=ref_KiqH713Hfa",
                    BlumAuthData    = "authData",
                    EarnedToday     = 1441.5,
                    TookDailyReward = true
                }
            });
            yield return SwaggerExample.Create("Empty slot", new ApiObjectResponse<AccountDTO>()
            {
                ok   = true,
                data = new()
                {
                    Username        = "",
                    Balance         = 0,
                    Tickets         = 0,
                    ReferralCount   = 0,
                    ReferralLink    = "",
                    BlumAuthData    = "",
                    EarnedToday     = 0,
                    TookDailyReward = false
                }
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
