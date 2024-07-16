using System.Security.Claims;

namespace AutoBlumFarmServer.Helpers
{
    public class Utils
    {
        public static Claim? GetUserClaim(IEnumerable<Claim> claims, string claim, out bool userAuthorized)
        {
            userAuthorized = true;
            var gotClaim = claims.FirstOrDefault(c => c.Type == claim);
            if (gotClaim == null)
            {
                userAuthorized = false;
            }

            return gotClaim;
        }

        public static int GetUserIdFromClaims(IEnumerable<Claim> claims, out bool userAuthorized)
        {
            int userId = 0;

            var gotClaim = GetUserClaim(claims, "Id", out userAuthorized);

            if (gotClaim != null && !int.TryParse(gotClaim.Value, out userId))
            {
                userAuthorized = false;
            }

            return userId;
        }
    }
}
