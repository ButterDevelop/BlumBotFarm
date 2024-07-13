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
                    new()
                    {
                        Username        = "UserName",
                        Balance         = 40193.7,
                        Tickets         = 4,
                        ReferralCount   = 2,
                        ReferralLink    = "t.me/BlumCryptoBot/app?startapp=ref_KiqH713Hfa",
                        BlumAuthData    = "authData",
                        EarnedToday     = 1441.5,
                        TookDailyReward = true
                    },
                    new()
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
