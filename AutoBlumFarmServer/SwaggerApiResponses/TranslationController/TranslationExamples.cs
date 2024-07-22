using BlumBotFarm.Translation;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Filters;

namespace AutoBlumFarmServer.SwaggerApiResponses.UserController
{
    public class GetTranslationsOkExample : IMultipleExamplesProvider<ApiObjectResponse<List<TranslationModel>?>>
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

    public class GetTranslationsBadExample : IMultipleExamplesProvider<ApiMessageResponse>
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

    public class GetAvailableLanguagesOkExample : IMultipleExamplesProvider<ApiObjectResponse<Dictionary<string, string>?>>
    {
        public IEnumerable<SwaggerExample<ApiObjectResponse<Dictionary<string, string>?>>> GetExamples()
        {
            Dictionary<string, string> dict = [];
            foreach (var key in TranslationHelper.Instance.AvailableLanguageCodes.Select(l => l.ToUpper()))
            {
                if (TranslationHelper.LanguageCodeToLanguageName.TryGetValue(key.ToLower(), out string? value)) dict.Add(key, value);
            }

            yield return SwaggerExample.Create("Available languages", new ApiObjectResponse<Dictionary<string, string>?>
            {
                ok   = true,
                data = dict
            });
        }
    }

    public class ChangeLanguageOkExample : IMultipleExamplesProvider<ApiMessageResponse>
    {
        public IEnumerable<SwaggerExample<ApiMessageResponse>> GetExamples()
        {
            yield return SwaggerExample.Create("Successfully changed language", new ApiMessageResponse
            {
                ok      = true,
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_LANGUAGE_WAS_CHANGED_SUCCESSFULLY%#")
            });
        }
    }

    public class ChangeLanguageBadExample : IMultipleExamplesProvider<ApiMessageResponse>
    {
        public IEnumerable<SwaggerExample<ApiMessageResponse>> GetExamples()
        {
            yield return SwaggerExample.Create("Language not found", new ApiMessageResponse
            {
                ok      = false,
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_LANGUAGE_NOT_FOUND%#")
            });
        }
    }
}
