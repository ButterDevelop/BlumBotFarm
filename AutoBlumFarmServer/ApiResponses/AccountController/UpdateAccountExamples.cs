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
            yield return SwaggerExample.Create("Validation failed: Custom Username", new ApiMessageResponse()
            {
                ok      = false,
                message = "Validation failed: Custom Username. Use 6-10 alphanumeric symbols."
            });
            yield return SwaggerExample.Create("Username is occupied", new ApiMessageResponse()
            {
                ok      = false,
                message = "This username is already occupied by your account or someone else's."
            });
            yield return SwaggerExample.Create("Validation failed: Country Code", new ApiMessageResponse()
            {
                ok      = false,
                message = "Validation failed: Country Code. No such country code."
            });
            yield return SwaggerExample.Create("Validation failed: Blum Telegram Auth", new ApiMessageResponse()
            {
                ok      = false,
                message = "Validation failed: Blum Telegram Auth. Check your string."
            });
            yield return SwaggerExample.Create("Can't add proxy", new ApiMessageResponse()
            {
                ok      = false,
                message = "Can't add proxy to proxy service. Please, try again later."
            });
            yield return SwaggerExample.Create("Can't download proxy", new ApiMessageResponse()
            {
                ok      = false,
                message = "Can't get proxy from proxy service. Please, try again later."
            });
            yield return SwaggerExample.Create("Wrong proxy format", new ApiMessageResponse()
            {
                ok      = false,
                message = "Proxy service has returned proxy in wrong format. Please, try again later."
            });
        }
    }
}
