using BlumBotFarm.Translation;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Filters;

namespace AutoBlumFarmServer.ApiResponses.UserController
{
    public class TranslationOkExample : IMultipleExamplesProvider<ApiObjectResponse<List<TranslationModel>?>>
    {
        public IEnumerable<SwaggerExample<ApiObjectResponse<List<TranslationModel>?>>> GetExamples()
        {
            string lang = "en";
            var filePath = Path.Combine("Translations", $"{lang}.json");
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var obj  = JsonConvert.DeserializeObject<List<TranslationModel>>(json);
                yield return SwaggerExample.Create("A translation", new ApiObjectResponse<List<TranslationModel>?>
                {
                    ok   = true,
                    data = obj
                });
            }
        }
    }

    public class TranslationBadExample : IMultipleExamplesProvider<ApiMessageResponse>
    {
        public IEnumerable<SwaggerExample<ApiMessageResponse>> GetExamples()
        {
            yield return SwaggerExample.Create("Translation was not found", new ApiMessageResponse
            {
                ok      = false,
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_TRANSLATION_NOT_FOUND%#")
            });
        }
    }
}
