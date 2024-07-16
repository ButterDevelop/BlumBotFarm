using AutoBlumFarmServer.Model;
using Swashbuckle.AspNetCore.Filters;

namespace AutoBlumFarmServer.ApiResponses.AccountController
{
    public class AllGeoOkExample : IMultipleExamplesProvider<ApiObjectResponse<AllGeoOutputModel>>
    {
        public IEnumerable<SwaggerExample<ApiObjectResponse<AllGeoOutputModel>>> GetExamples()
        {
            AllGeoOutputModel output = new()
            {
                Geos = []
            };
            foreach (var item in Config.Instance.GEO_PROXY_SELLER)
            {
                output.Geos.Add(new()
                {
                    CountryCode    = item.Key,
                    CountryName    = item.Value.countryName,
                    TimezoneOffset = item.Value.timezoneOffset
                });
            }

            yield return SwaggerExample.Create("All good", new ApiObjectResponse<AllGeoOutputModel>()
            {
                ok   = true,
                data = output
            });
            yield return SwaggerExample.Create("Empty list", new ApiObjectResponse<AllGeoOutputModel>()
            {
                ok   = true,
                data = new() { Geos = [] }
            });
        }
    }
}
