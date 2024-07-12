using Swashbuckle.AspNetCore.Filters;

namespace AutoBlumFarmServer.ApiResponses.TelegramAuthController
{
    public class TelegramAuthOkExample : IMultipleExamplesProvider<ApiObjectResponse<string>>
    {
        public IEnumerable<SwaggerExample<ApiObjectResponse<string>>> GetExamples()
        {
            yield return SwaggerExample.Create("Good, got the token", new ApiObjectResponse<string>()
            {
                ok    = true,
                data = "ey0345asdfxcvsdfqertTOKENwtfdgdfgsf"
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
        }
    }
}
