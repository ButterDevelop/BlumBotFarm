using System.Security.Claims;

namespace AutoBlumFarmServer
{
    public class Utils
    {
        private readonly static Random random = new();
        private const string ALPHABET_NUMERIC_CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

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

        public static string RandomString(int length)
        {
            return new string(Enumerable.Repeat(ALPHABET_NUMERIC_CHARS, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
