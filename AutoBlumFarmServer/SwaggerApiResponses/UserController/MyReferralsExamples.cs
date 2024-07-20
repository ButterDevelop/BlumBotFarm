using AutoBlumFarmServer.Model;
using Swashbuckle.AspNetCore.Filters;

namespace AutoBlumFarmServer.SwaggerApiResponses.UserController
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
                    new() { Id = 1, FirstName = "Johnny", LastName = "Cage", HostEarnings = 0M,   },
                    new() { Id = 2, FirstName = "Elon",   LastName = "Musk", HostEarnings = 10.5M },
                    new() { Id = 3, FirstName = "Dalai",  LastName = "Lama", HostEarnings = 1M,   }
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
