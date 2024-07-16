using AutoBlumFarmServer.DTO;
using Swashbuckle.AspNetCore.Filters;

namespace AutoBlumFarmServer.ApiResponses.AccountController
{
    public class GetAllAccountsOkExample : IMultipleExamplesProvider<ApiObjectResponse<List<AccountDTO>>>
    {
        public IEnumerable<SwaggerExample<ApiObjectResponse<List<AccountDTO>>>> GetExamples()
        {
            yield return SwaggerExample.Create("2 accounts + 1 empty slot", new ApiObjectResponse<List<AccountDTO>>()
            {
                ok = true,
                data =
                [
                    new()
                    {
                        Id              = 1,
                        CustomUsername  = "My account",
                        BlumUsername    = "UserName1",
                        Balance         = 40193.7,
                        Tickets         = 4,
                        ReferralCount   = 2,
                        ReferralLink    = "t.me/BlumCryptoBot/app?startapp=ref_KiqH713Hfa",
                        BlumAuthData    = "authData1",
                        EarnedToday     = 1441.5,
                        TookDailyReward = true,
                        NearestWorkIn   = "-00:03:45",
                        CountryCode     = "US",
                        LastStatus      = "OK"
                    },
                    new()
                    {
                        Id              = 2,
                        CustomUsername  = "My second account",
                        BlumUsername    = "UserName2",
                        Balance         = 53219.7,
                        Tickets         = 3,
                        ReferralCount   = 0,
                        ReferralLink    = "t.me/BlumCryptoBot/app?startapp=ref_13HfkIQH7a",
                        BlumAuthData    = "authData2",
                        EarnedToday     = 0,
                        TookDailyReward = false,
                        NearestWorkIn   = "03:05:32",
                        CountryCode     = "BY",
                        LastStatus      = "Can't authenticate"
                    },
                    new()
                    {
                        Id              = 3,
                        CustomUsername  = "",
                        BlumUsername    = "",
                        Balance         = 0,
                        Tickets         = 0,
                        ReferralCount   = 0,
                        ReferralLink    = "",
                        BlumAuthData    = "",
                        EarnedToday     = 0,
                        TookDailyReward = false,
                        NearestWorkIn   = "Unknown",
                        CountryCode     = "CZ",
                        LastStatus      = ""
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
