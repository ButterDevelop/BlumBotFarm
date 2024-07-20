using Newtonsoft.Json;
using Serilog;

namespace BlumBotFarm.Translation
{
    public class TranslationHelper
    {
        public readonly static TranslationHelper Instance = new();

        public const string DEFAULT_LANG_CODE = "en";

        private readonly Dictionary<string, Dictionary<string, string>> _translator;

        public TranslationHelper()
        {
            _translator = [];

            const string directoryName = "TranslationsAPI";
            if (Directory.Exists(directoryName))
            {
                var files = Directory.GetFiles(directoryName);
                foreach (var file in files)
                {
                    if (!file.Contains(".json")) continue;

                    var json = File.ReadAllText(file);
                    var obj  = JsonConvert.DeserializeObject<List<TranslationModel>>(json);

                    if (obj != null)
                    {
                        Dictionary<string, string> localTranslator = [];
                        foreach (var item in obj)
                        {
                            if (!localTranslator.ContainsKey(item.Mask)) localTranslator.Add(item.Mask, item.Text);
                        }

                        string langCode = file.Replace(".json", "").Replace(directoryName + Path.DirectorySeparatorChar, "");
                        _translator.Add(langCode, localTranslator);
                    }
                }
            }
            
            if (!Directory.Exists(directoryName) || _translator.Count == 0 || _translator.Keys.Select(k => k.Length).Any(l => l > 3))
            {
                Log.Error("Translation Helper: can't find TranslationsAPI folder!!!");
            }
            else
            {
                Log.Information("Translation Helper was loaded successfully.");
            }
        }

        public string Translate(string langCode, string mask)
        {
            if ((!_translator.TryGetValue(langCode, out Dictionary<string, string>? value) || !value.TryGetValue(mask, out string? result)))
            {
                if (langCode != DEFAULT_LANG_CODE)
                {
                    return Translate(DEFAULT_LANG_CODE, mask);
                }
                else return mask;
            }

            return result;
        }

        public List<string> AvailableLanguageCodes
        {
            get
            {
                return _translator.Keys.ToList();
            }
        }
    }
}
