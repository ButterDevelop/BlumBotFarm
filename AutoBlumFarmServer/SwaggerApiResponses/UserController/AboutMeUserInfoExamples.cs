using AutoBlumFarmServer.DTO;
using Swashbuckle.AspNetCore.Filters;

namespace AutoBlumFarmServer.SwaggerApiResponses.UserController
{
    public class AboutMeUserInfoOkExample : IMultipleExamplesProvider<ApiObjectResponse<UserDTO>>
    {
        public IEnumerable<SwaggerExample<ApiObjectResponse<UserDTO>>> GetExamples()
        {
            yield return SwaggerExample.Create("About current user", new ApiObjectResponse<UserDTO>()
            {
                ok   = true,
                data = new()
                {
                    Id                  = 1,
                    TelegramUserId      = 11223344,
                    FirstName           = "Karl",
                    LastName            = "Marx",
                    BalanceUSD          = 100,
                    LanguageCode        = "en",
                    OwnReferralCode     = "pKvp3NIdiK",
                    PhotoUrl            = "https://cdn.telegram.org/avatars/avatar176.png",
                    AccountsBalancesSum = 1185.44,
                    HadTrial            = true
                }
            });
            yield return SwaggerExample.Create("Without avatar", new ApiObjectResponse<UserDTO>()
            {
                ok   = true,
                data = new()
                {
                    Id                  = 2,
                    TelegramUserId      = 22334455,
                    FirstName           = "Elon",
                    LastName            = "Musk",
                    BalanceUSD          = 100000,
                    LanguageCode        = "en",
                    OwnReferralCode     = "IpiKpKv3Nd",
                    PhotoUrl            = "",
                    AccountsBalancesSum = 0,
                    HadTrial            = false
                }
            });
        }
    }
}
