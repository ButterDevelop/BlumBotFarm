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
                message = "The invoice to top up your balance was sent to you."
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
                message = "Something went wrong. Please, try again later."
            });
            yield return SwaggerExample.Create("Error with Telegram", new ApiMessageResponse()
            {
                ok      = false,
                message = "Something went wrong with Telegram. Please, try again later."
            });
        }
    }
}
