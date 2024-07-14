using AutoBlumFarmServer.Model;
using Swashbuckle.AspNetCore.Filters;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AutoBlumFarmServer.ApiResponses.PurchaseController
{
    public class BuyAccountsSlotsOkExample : IMultipleExamplesProvider<ApiMessageResponse>
    {
        public IEnumerable<SwaggerExample<ApiMessageResponse>> GetExamples()
        {
            yield return SwaggerExample.Create("Successful buy", new ApiMessageResponse
            {
                ok      = true,
                message = "You have bought 1 slot successfully!"
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
                message = "The amount of slots you specified is wrong."
            });
            yield return SwaggerExample.Create("Not enough money", new ApiMessageResponse
            {
                ok      = false,
                message = "Not enough money on your balance."
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
                message = "The amount of slots you specified is wrong."
            });
        }
    }
}
