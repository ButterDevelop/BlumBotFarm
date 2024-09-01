using BlumBotFarm.Translation;
using Swashbuckle.AspNetCore.Filters;

namespace AutoBlumFarmServer.SwaggerApiResponses.UserController
{
    public class ActivateTrialOkExample : IMultipleExamplesProvider<ApiMessageResponse>
    {
        public IEnumerable<SwaggerExample<ApiMessageResponse>> GetExamples()
        {
            yield return SwaggerExample.Create("Activated trial successfully", new ApiMessageResponse()
            {
                ok      = true,
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_ACTIVATED_TRIAL_SUCCESSFULLY%#")
            });
        }
    }

    public class ActivateTrialBadExample : IMultipleExamplesProvider<ApiMessageResponse>
    {
        public IEnumerable<SwaggerExample<ApiMessageResponse>> GetExamples()
        {
            yield return SwaggerExample.Create("Trial was already used", new ApiMessageResponse()
            {
                ok      = false,
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_ALREADY_USED_TRIAL%#")
            });
        }
    }
}
