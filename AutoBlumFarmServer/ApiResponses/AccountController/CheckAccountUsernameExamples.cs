using BlumBotFarm.Translation;
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
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_USERNAME_IS_AVAILABLE%#")
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
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_USERNAME_IS_OCCUPIED%#")
            });
            yield return SwaggerExample.Create("Validation error", new ApiMessageResponse()
            {
                ok      = false,
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_USERNAME_VALIDATION_FAILED%#")
            });
        }
    }
}
