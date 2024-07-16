using AutoBlumFarmServer.Model;
using BlumBotFarm.Translation;
using Swashbuckle.AspNetCore.Filters;

namespace AutoBlumFarmServer.ApiResponses.PurchaseController
{
    public class BuyAccountsSlotsOkExample : IMultipleExamplesProvider<ApiMessageResponse>
    {
        public IEnumerable<SwaggerExample<ApiMessageResponse>> GetExamples()
        {
            yield return SwaggerExample.Create("Successful buy", new ApiMessageResponse
            {
                ok      = true,
                message = string.Format(
                              TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, 
                              "#%MESSAGE_YOU_HAVE_BOUGHT_SLOTS_SUCCESSFULLY%#"),
                          10)
            });
        }
    }

    public class BuyAccountsSlotsBadExample : IMultipleExamplesProvider<ApiMessageResponse>
    {
        public IEnumerable<SwaggerExample<ApiMessageResponse>> GetExamples()
        {
            yield return SwaggerExample.Create("Wrong amount number", new ApiMessageResponse
            {
                ok      = false,
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_THE_AMOUNT_OF_STARS_IS_WRONG%#")
            });
            yield return SwaggerExample.Create("Not enough money", new ApiMessageResponse
            {
                ok      = false,
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_NOT_ENOUGH_MONEY_ON_YOUR_BALANCE%#")
            });
        }
    }

    public class PreBuyAccountsSlotsOkExample : IMultipleExamplesProvider<ApiObjectResponse<PreBuyAccountsSlotsOutputModel>>
    {
        public IEnumerable<SwaggerExample<ApiObjectResponse<PreBuyAccountsSlotsOutputModel>>> GetExamples()
        {
            yield return SwaggerExample.Create("Successful answer", new ApiObjectResponse<PreBuyAccountsSlotsOutputModel>
            {
                ok   = true,
                data = new()
                {
                    price    = 1.98M,
                    discount = 0M
                }
            });
        }
    }

    public class PreBuyAccountsSlotsBadExample : IMultipleExamplesProvider<ApiMessageResponse>
    {
        public IEnumerable<SwaggerExample<ApiMessageResponse>> GetExamples()
        {
            yield return SwaggerExample.Create("Wrong amount number", new ApiMessageResponse
            {
                ok      = false,
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_THE_AMOUNT_OF_STARS_IS_WRONG%#")
            });
        }
    }
}
