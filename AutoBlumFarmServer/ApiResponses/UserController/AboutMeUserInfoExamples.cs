using AutoBlumFarmServer.DTO;
using Swashbuckle.AspNetCore.Filters;

namespace AutoBlumFarmServer.ApiResponses.UserController
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
                    Id              = 1,
                    TelegramUserId  = 11223344,
                    BalanceUSD      = 100,
                    LanguageCode    = "en",
                    OwnReferralCode = "pKvp3NIdiK"
                }
            });
        }
    }
}
