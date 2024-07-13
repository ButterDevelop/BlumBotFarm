using Swashbuckle.AspNetCore.Filters;

namespace AutoBlumFarmServer.ApiResponses.AccountController
{
    public class UpdateAccountOkExample : IMultipleExamplesProvider<ApiMessageResponse>
    {
        public IEnumerable<SwaggerExample<ApiMessageResponse>> GetExamples()
        {
            yield return SwaggerExample.Create("Update was successful", new ApiMessageResponse()
            {
                ok      = true,
                message = "This username for your account is available."
            });
        }
    }

    public class UpdateAccountBadExample : IMultipleExamplesProvider<ApiMessageResponse>
    {
        public IEnumerable<SwaggerExample<ApiMessageResponse>> GetExamples()
        {
            yield return SwaggerExample.Create("No such account", new ApiMessageResponse()
            {
                ok      = false,
                message = "No such account that belongs to our user."
            });
            yield return SwaggerExample.Create("Validation error", new ApiMessageResponse()
            {
                ok      = false,
                message = "Validation failed. Use 6-10 alphanumeric symbols."
            });
            yield return SwaggerExample.Create("Username is occupied", new ApiMessageResponse()
            {
                ok      = false,
                message = "This username is already occupied by your account or someone else's."
            });
        }
    }
}
