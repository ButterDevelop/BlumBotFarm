using BlumBotFarm.Core.Models;
using BlumBotFarm.Translation;
using Swashbuckle.AspNetCore.Filters;

namespace AutoBlumFarmServer.ApiResponses.TelegramAuthController
{
    public class StarsPaymentTransactionOkExample : IMultipleExamplesProvider<ApiMessageResponse>
    {
        public IEnumerable<SwaggerExample<ApiMessageResponse>> GetExamples()
        {
            yield return SwaggerExample.Create("Invoice created", new ApiMessageResponse()
            {
                ok      = true,
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_THE_TOPUP_BALANCE_INVOICE_WAS_SENT%#")
            });
        }
    }

    public class StarsPaymentTransactionBadExample : IMultipleExamplesProvider<ApiMessageResponse>
    {
        public IEnumerable<SwaggerExample<ApiMessageResponse>> GetExamples()
        {
            yield return SwaggerExample.Create("Error with DB somewhere", new ApiMessageResponse()
            {
                ok      = false,
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_SOMETHING_WENT_WRONG%#")
            });
            yield return SwaggerExample.Create("Error with Telegram", new ApiMessageResponse()
            {
                ok      = false,
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_SOMETHING_WENT_WRONG_WITH_TELEGRAM%#")
            });
        }
    }

    public class ConvertStarsToUsdOkExample : IMultipleExamplesProvider<ApiObjectResponse<decimal>>
    {
        public IEnumerable<SwaggerExample<ApiObjectResponse<decimal>>> GetExamples()
        {
            yield return SwaggerExample.Create("Stars converted to USD", new ApiObjectResponse<decimal>()
            {
                ok   = true,
                data = 9.14M
            });
        }
    }

    public class ConvertUsdToStarsOkExample : IMultipleExamplesProvider<ApiObjectResponse<int>>
    {
        public IEnumerable<SwaggerExample<ApiObjectResponse<int>>> GetExamples()
        {
            yield return SwaggerExample.Create("USD converted to Stars", new ApiObjectResponse<int>()
            {
                ok   = true,
                data = 4
            });
        }
    }

    public class ConvertCurrenciesBadExample : IMultipleExamplesProvider<ApiMessageResponse>
    {
        public IEnumerable<SwaggerExample<ApiMessageResponse>> GetExamples()
        {
            yield return SwaggerExample.Create("Wrong number specified", new ApiMessageResponse
            {
                ok      = false,
                message = TranslationHelper.Instance.Translate(TranslationHelper.DEFAULT_LANG_CODE, "#%MESSAGE_WRONG_NUMBER_SPECIFIED%#")
            });
        }
    }

    public class MyPaymentTransactionsOkExample : IMultipleExamplesProvider<ApiObjectResponse<List<StarsPaymentDTO>>>
    {
        public IEnumerable<SwaggerExample<ApiObjectResponse<List<StarsPaymentDTO>>>> GetExamples()
        {
            yield return SwaggerExample.Create("List of payment transactions", new ApiObjectResponse<List<StarsPaymentDTO>>()
            {
                ok   = true,
                data = 
                [
                    new()
                    {
                        AmountStars       = 50,
                        AmountUsd         = 1.12M,
                        CreatedDateTime   = DateTime.UtcNow.AddHours(-2),
                        IsCompleted       = false,
                        CompletedDateTime = DateTime.UtcNow.AddHours(-2),
                    },
                    new()
                    {
                        AmountStars       = 1000,
                        AmountUsd         = 22.4M,
                        CreatedDateTime   = DateTime.UtcNow.AddHours(-3),
                        IsCompleted       = true,
                        CompletedDateTime = DateTime.UtcNow.AddSeconds(-187),
                    },
                ]
            });
            yield return SwaggerExample.Create("Empty list of payment transactions", new ApiObjectResponse<List<StarsPaymentDTO>>()
            {
                ok   = true,
                data = []
            });
        }
    }
}
