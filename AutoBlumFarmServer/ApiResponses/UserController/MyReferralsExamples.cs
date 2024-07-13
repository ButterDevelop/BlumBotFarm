using AutoBlumFarmServer.Model;
using Swashbuckle.AspNetCore.Filters;

namespace AutoBlumFarmServer.ApiResponses.UserController
{
    public class MyReferralsOkExample : IMultipleExamplesProvider<ApiObjectResponse<List<ReferralsOutputModel>>>
    {
        public IEnumerable<SwaggerExample<ApiObjectResponse<List<ReferralsOutputModel>>>> GetExamples()
        {
            yield return SwaggerExample.Create("A few referrals", new ApiObjectResponse<List<ReferralsOutputModel>>()
            {
                ok   = true,
                data =
                [
                    new() { FirstName = "Johnny", LastName = "Cage", HostEarnings = 0M,    PhotoUrl = "" },
                    new() { FirstName = "Elon",   LastName = "Musk", HostEarnings = 10.5M, PhotoUrl = "https://cdn.telegram.org/avatars/avatar314.png" },
                    new() { FirstName = "Dalai",  LastName = "Lama", HostEarnings = 1M,    PhotoUrl = "" }
                ]
            });
            yield return SwaggerExample.Create("No referrals", new ApiObjectResponse<List<ReferralsOutputModel>>()
            {
                ok   = true,
                data = []
            });
        }
    }
}
