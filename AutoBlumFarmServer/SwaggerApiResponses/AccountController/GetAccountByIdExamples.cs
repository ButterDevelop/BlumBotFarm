using AutoBlumFarmServer.DTO;
using BlumBotFarm.Translation;
using Swashbuckle.AspNetCore.Filters;

namespace AutoBlumFarmServer.SwaggerApiResponses.AccountController
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
                    NearestWorkIn   = "06:55:33",
                    CountryCode     = "US",
                    LastStatus      = "OK",
                    IsTrial         = false,
                    TrialExpires    = DateTime.UtcNow
                }
            });
            yield return SwaggerExample.Create("Minus nearest work in", new ApiObjectResponse<AccountDTO>()
            {
                ok   = true,
                data = new()
                {
                    Id              = 2,
                    CustomUsername  = "My second account",
                    BlumUsername    = "UserName2",
                    Balance         = 53219.7,
                    Tickets         = 3,
                    ReferralCount   = 0,
                    ReferralLink    = "t.me/BlumCryptoBot/app?startapp=ref_13HfkIQH7a",
                    BlumAuthData    = "authData2",
                    EarnedToday     = 275,
                    TookDailyReward = false,
                    NearestWorkIn   = "-00:00:31",
                    CountryCode     = "BY",
                    LastStatus      = "Can't authenticate",
                    IsTrial         = true,
                    TrialExpires    = DateTime.UtcNow.AddHours(24 * 6 + 18)
                },
            });
            yield return SwaggerExample.Create("Empty slot", new ApiObjectResponse<AccountDTO>()
            {
                ok   = true,
                data = new()
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
                    NearestWorkIn   = "-",
                    CountryCode     = "CZ",
                    LastStatus      = "",
                    IsTrial         = false,
                    TrialExpires    = DateTime.UtcNow
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
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_NO_SUCH_ACCOUNT%#")
            });
        }
    }
}
