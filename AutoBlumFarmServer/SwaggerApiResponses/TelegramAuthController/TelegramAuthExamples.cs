using AutoBlumFarmServer.Model;
using Swashbuckle.AspNetCore.Filters;

namespace AutoBlumFarmServer.SwaggerApiResponses.TelegramAuthController
{
    public class TelegramAuthOkExample : IMultipleExamplesProvider<ApiObjectResponse<TGAuthOutputModel>>
    {
        public IEnumerable<SwaggerExample<ApiObjectResponse<TGAuthOutputModel>>> GetExamples()
        {
            yield return SwaggerExample.Create("Good, got the token and info", new ApiObjectResponse<TGAuthOutputModel>()
            {
                ok   = true,
                data = new() 
                {
                    token        = "ey761387TOKENakjdh612",
                    expires      = DateTime.UtcNow.AddHours(Config.Instance.JWT_LIVE_HOURS),
                    languageCode = "en",
                }
            });
        }
    }

    public class TelegramAuthBadExample : IMultipleExamplesProvider<ApiMessageResponse>
    {
        public IEnumerable<SwaggerExample<ApiMessageResponse>> GetExamples()
        {
            yield return SwaggerExample.Create("Cannot verify", new ApiMessageResponse()
            {
                ok      = false,
                message = "Verification failed."
            });
            yield return SwaggerExample.Create("No data", new ApiMessageResponse()
            {
                ok      = false,
                message = "No data provided."
            });
        }
    }
}
