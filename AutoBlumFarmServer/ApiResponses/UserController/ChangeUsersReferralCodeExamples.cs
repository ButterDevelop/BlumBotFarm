using Swashbuckle.AspNetCore.Filters;

namespace AutoBlumFarmServer.ApiResponses.UserController
{
    public class ChangeUsersReferralCodeOkExample : IMultipleExamplesProvider<ApiMessageResponse>
    {
        public IEnumerable<SwaggerExample<ApiMessageResponse>> GetExamples()
        {
            yield return SwaggerExample.Create("Referral code was updated", new ApiMessageResponse()
            {
                ok      = true,
                message = "Your own referral code was updated successfully."
            });
        }
    }

    public class ChangeUsersReferralCodeBadExample : IMultipleExamplesProvider<ApiMessageResponse>
    {
        public IEnumerable<SwaggerExample<ApiMessageResponse>> GetExamples()
        {
            yield return SwaggerExample.Create("Validation error", new ApiMessageResponse()
            {
                ok      = false,
                message = "Validation failed. Use 10 alphanumeric symbols."
            });
        }
    }
}
