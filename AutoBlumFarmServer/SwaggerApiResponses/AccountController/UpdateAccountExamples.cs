using BlumBotFarm.Translation;
using Swashbuckle.AspNetCore.Filters;

namespace AutoBlumFarmServer.SwaggerApiResponses.AccountController
{
    public class UpdateAccountOkExample : IMultipleExamplesProvider<ApiMessageResponse>
    {
        public IEnumerable<SwaggerExample<ApiMessageResponse>> GetExamples()
        {
            yield return SwaggerExample.Create("Update was successful", new ApiMessageResponse()
            {
                ok      = true,
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_USERNAME_IS_AVAILABLE%#")
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
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_NO_SUCH_ACCOUNT%#")
            });
            yield return SwaggerExample.Create("Validation failed: Custom Username", new ApiMessageResponse()
            {
                ok      = false,
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_CUSTOM_USERNAME_VALIDATION_FAILED%#")
            });
            yield return SwaggerExample.Create("Username is occupied", new ApiMessageResponse()
            {
                ok      = false,
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_USERNAME_IS_OCCUPIED%#")
            });
            yield return SwaggerExample.Create("Validation failed: Country Code", new ApiMessageResponse()
            {
                ok      = false,
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_COUNTRY_CODE_VALIDATION_FAILED%#")
            });
            yield return SwaggerExample.Create("Validation failed: Blum Telegram Auth", new ApiMessageResponse()
            {
                ok      = false,
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_BLUM_TELEGRAM_AUTH_VALIDATION_FAILED%#")
            });
            yield return SwaggerExample.Create("Can't add proxy", new ApiMessageResponse()
            {
                ok      = false,
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_CANT_ADD_PROXY_TO_PROXY_SERVICE%#")
            });
            yield return SwaggerExample.Create("Can't download proxy", new ApiMessageResponse()
            {
                ok      = false,
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_CANT_GET_PROXY_FROM_PROXY_SERVICE%#")
            });
            yield return SwaggerExample.Create("Wrong proxy format", new ApiMessageResponse()
            {
                ok      = false,
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_PROXY_SERVICE_RETURNED_WRONG_FORMAT%#")
            });
        }
    }
}
