using Swashbuckle.AspNetCore.Filters;

namespace AutoBlumFarmServer.ApiResponses
{
    public class BadAuthExample : IMultipleExamplesProvider<ApiMessageResponse>
    {
        public IEnumerable<SwaggerExample<ApiMessageResponse>> GetExamples()
        {
            yield return SwaggerExample.Create("Unauthorized", new ApiMessageResponse()
            {
                ok = true,
                message = "No auth."
            });
        }
    }
}
