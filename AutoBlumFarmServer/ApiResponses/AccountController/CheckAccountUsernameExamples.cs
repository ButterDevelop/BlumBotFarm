using Swashbuckle.AspNetCore.Filters;

namespace AutoBlumFarmServer.ApiResponses.AccountController
{
    public class CheckAccountUsernameOkExample : IMultipleExamplesProvider<ApiMessageResponse>
    {
        public IEnumerable<SwaggerExample<ApiMessageResponse>> GetExamples()
        {
            yield return SwaggerExample.Create("Username is available", new ApiMessageResponse()
            {
                ok      = true,
                message = "This username for your account is available."
            });
        }
    }

    public class CheckAccountUsernameBadExample : IMultipleExamplesProvider<ApiMessageResponse>
    {
        public IEnumerable<SwaggerExample<ApiMessageResponse>> GetExamples()
        {
            yield return SwaggerExample.Create("Username is NOT available", new ApiMessageResponse()
            {
                ok      = false,
                message = "This username is already occupied by one of your accounts."
            });
            yield return SwaggerExample.Create("Validation error", new ApiMessageResponse()
            {
                ok      = false,
                message = "Validation failed. Use 6-10 alphanumeric symbols."
            });
        }
    }
}
