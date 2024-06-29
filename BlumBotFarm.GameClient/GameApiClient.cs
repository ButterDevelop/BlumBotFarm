using BlumBotFarm.Core.Models;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Net;

namespace BlumBotFarm.GameClient
{
    public class GameApiClient
    {
        private const string BASE_GAMING_API_URL               = "https://game-domain.blum.codes/api/v1/",
                             BASE_GATEWAY_API_URL              = "https://gateway.blum.codes/v1/",
                             HTML_PAGE_URL                     = "https://telegram.blum.codes/",
                             ABOUT_ME_REQUEST_ENDPOINT         = "user/me",
                             REFRESH_AUTH_REQUEST_ENDPOINT     = "auth/refresh",
                             GET_DAILY_REWARD_REQUEST_ENDPOINT = "daily-reward?offset=",
                             START_GAME_REQUEST_ENDPOINT       = "game/play",
                             END_GAME_REQUEST_ENDPOINT         = "game/claim",
                             GET_USER_INFO_REQUEST_ENDPOINT    = "user/balance",
                             START_FARMING_REQUEST_ENDPOINT    = "farming/start",
                             CLAIM_FARMING_REQUEST_ENDPOINT    = "farming/claim",
                             TASKS_INFO_REQUEST_ENDPOINT       = "tasks",
                             START_TASK_REQUEST_ENDPOINT       = "tasks/{0}/start",
                             CLAIM_TASK_REQUEST_ENDPOINT       = "tasks/{0}/claim",
                             FRIENDS_CLAIM_REQUEST_ENDPOINT    = "friends/claim";

        private readonly Dictionary<string, string> _commonHeaders = new Dictionary<string, string>
        {
            { "Accept",             "application/json, text/plain, */*" },
            { "Sec-fetch-site",     "cross-site" },
            { "Sec-fetch-mode",     "cors" },
            { "Sec-fetch-dest",     "empty" }
        };

        public GameApiClient()
        {
            
        }

        public static Dictionary<string, string> GetUniqueHeaders(Dictionary<string, string> sourceHeaders, string accessToken)
        {
            var headers = sourceHeaders.ToDictionary(entry => entry.Key, entry => entry.Value);
            if (!string.IsNullOrEmpty(accessToken))
            {
                headers.Add("Authorization", "Bearer " + accessToken);
            }

            return headers;
        }

        public bool GetMainPageHTML(Account account)
        {
            var headers = GetUniqueHeaders(_commonHeaders, string.Empty);
            if (headers.ContainsKey("Accept")) headers["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application07";

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                await HTTPController.SendRequestAsync(HTML_PAGE_URL, RequestType.GET, account.Proxy, headers,
                                                      parameters: null, parametersString: null, parametersContentType: null, referer: null,
                                                      account.UserAgent)
            );
            (string? answer, HttpStatusCode responseStatusCode) = taskResult.Result;

            return answer != null && answer.Length > 0 && responseStatusCode == HttpStatusCode.OK;
        }

        public ApiResponse GetAboutMeInfo(Account account)
        {
            var headers = GetUniqueHeaders(_commonHeaders, account.AccessToken);

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                await HTTPController.SendRequestAsync(BASE_GATEWAY_API_URL + ABOUT_ME_REQUEST_ENDPOINT,
                                                      RequestType.GET, account.Proxy, headers,
                                                      parameters: null, parametersString: null, parametersContentType: null, referer: null,
                                                      account.UserAgent)
            );
            (string? jsonAnswer, HttpStatusCode responseStatusCode) = taskResult.Result;

            if (responseStatusCode == HttpStatusCode.Unauthorized)
            {
                Log.Error($"GameApiClient GetAboutMeInfo (Account with Id: {account.Id}, Username: {account.Username}) responseStatusCode - Unauthorized!");
                return ApiResponse.Unauthorized;
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient GetAboutMeInfo (Account with Id: {account.Id}, Username: {account.Username}) JSON answer: {jsonAnswer}");
                return ApiResponse.Error;
            }

            return ApiResponse.Success;
        }

        public (ApiResponse response, string accessToken, string refreshToken) RefreshAuth(Account account)
        {
            var headers = GetUniqueHeaders(_commonHeaders, string.Empty);

            string parametersString      = $"{{\"refresh\":\"{account.RefreshToken}\"}}";
            string parametersContentType = "application/json";

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                await HTTPController.SendRequestAsync(BASE_GATEWAY_API_URL + REFRESH_AUTH_REQUEST_ENDPOINT,
                                                      RequestType.POST, account.Proxy, headers,
                                                      parameters: null, parametersString, parametersContentType, referer: null,
                                                      account.UserAgent)
            );
            (string? jsonAnswer, HttpStatusCode responseStatusCode) = taskResult.Result;

            if (responseStatusCode == HttpStatusCode.Unauthorized)
            {
                Log.Error($"GameApiClient RefreshAuth (Account with Id: {account.Id}, Username: {account.Username}) responseStatusCode - Unauthorized!");
                return (ApiResponse.Unauthorized, string.Empty, string.Empty);
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient RefreshAuth (Account with Id: {account.Id}, Username: {account.Username}) JSON answer: {jsonAnswer}");
                return (ApiResponse.Error, string.Empty, string.Empty);
            }

            try
            {
                dynamic json = JObject.Parse(jsonAnswer.Replace("'", "\\'").Replace("\"", "'"));

                string accessToken  = json.access;
                string refreshToken = json.refresh;

                return (ApiResponse.Success, accessToken, refreshToken);
            }
            catch (RuntimeBinderException ex)
            {
                Log.Error($"GameApiClient RefreshAuth (Account with Id: {account.Id}, Username: {account.Username}) Exception: {ex}");
                return (ApiResponse.Error, string.Empty, string.Empty);
            }
        }

        public ApiResponse GetDailyReward(Account account)
        {
            var headers = GetUniqueHeaders(_commonHeaders, account.AccessToken);

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                await HTTPController.SendRequestAsync(BASE_GAMING_API_URL + GET_DAILY_REWARD_REQUEST_ENDPOINT + account.TimezoneOffset.ToString(),
                                                      RequestType.POST, account.Proxy, headers,
                                                      parameters: null, parametersString: null, parametersContentType: null, referer: null,
                                                      account.UserAgent)
            );
            (string? jsonAnswer, HttpStatusCode responseStatusCode) = taskResult.Result;

            if (responseStatusCode == HttpStatusCode.Unauthorized)
            {
                Log.Error($"GameApiClient GetDailyReward (Account with Id: {account.Id}, Username: {account.Username}) responseStatusCode - Unauthorized!");
                return ApiResponse.Unauthorized;
            }

            if (jsonAnswer != null && jsonAnswer.Contains("same day", StringComparison.CurrentCultureIgnoreCase))
            {
                Log.Information($"GameApiClient GetDailyReward (Account with Id: {account.Id}, Username: {account.Username}). Same day! JSON answer: {jsonAnswer}");
                return ApiResponse.Success;
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient GetDailyReward (Account with Id: {account.Id}, Username: {account.Username}) JSON answer: {jsonAnswer}");
                return ApiResponse.Error;
            }
            
            return ApiResponse.Success;
        }

        public (ApiResponse response, string gameId) StartGame(Account account)
        {
            var headers = GetUniqueHeaders(_commonHeaders, account.AccessToken);

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                await HTTPController.SendRequestAsync(BASE_GAMING_API_URL + START_GAME_REQUEST_ENDPOINT,
                                                      RequestType.POST, account.Proxy, headers,
                                                      parameters: null, parametersString: null, parametersContentType: null, referer: null,
                                                      account.UserAgent)
            );
            (string? jsonAnswer, HttpStatusCode responseStatusCode) = taskResult.Result;

            if (responseStatusCode == HttpStatusCode.Unauthorized)
            {
                Log.Error($"GameApiClient StartGame (Account with Id: {account.Id}, Username: {account.Username}) responseStatusCode - Unauthorized!");
                return (ApiResponse.Unauthorized, string.Empty);
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient StartGame (Account with Id: {account.Id}, Username: {account.Username}) JSON answer: {jsonAnswer}");
                return (ApiResponse.Error, string.Empty);
            }

            try
            {
                dynamic json = JObject.Parse(jsonAnswer.Replace("'", "\\'").Replace("\"", "'"));

                string gameId = json.gameId;

                return (ApiResponse.Success, gameId);
            }
            catch (RuntimeBinderException ex)
            {
                Log.Error($"GameApiClient StartGame (Account with Id: {account.Id}, Username: {account.Username}) Exception: {ex}");
                return (ApiResponse.Error, string.Empty);
            }
        }

        public ApiResponse EndGame(Account account, string gameId, int points)
        {
            var headers = GetUniqueHeaders(_commonHeaders, account.AccessToken);

            string parametersString      = $"{{\"gameId\":\"{gameId}\",\"points\":{points}}}";
            string parametersContentType = "application/json";

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                await HTTPController.SendRequestAsync(BASE_GAMING_API_URL + END_GAME_REQUEST_ENDPOINT,
                                                      RequestType.POST, account.Proxy, headers,
                                                      parameters: null, parametersString, parametersContentType, referer: null,
                                                      account.UserAgent)
            );
            (string? jsonAnswer, HttpStatusCode responseStatusCode) = taskResult.Result;

            if (responseStatusCode == HttpStatusCode.Unauthorized)
            {
                Log.Error($"GameApiClient EndGame (Account with Id: {account.Id}, Username: {account.Username}) responseStatusCode - Unauthorized!");
                return ApiResponse.Unauthorized;
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient EndGame (Account with Id: {account.Id}, Username: {account.Username}) JSON answer: {jsonAnswer}");
                return ApiResponse.Error;
            }

            return ApiResponse.Success;
        }

        public (ApiResponse response, double balance, int tickets) GetUserInfo(Account account)
        {
            var headers = GetUniqueHeaders(_commonHeaders, account.AccessToken);

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                await HTTPController.SendRequestAsync(BASE_GAMING_API_URL + GET_USER_INFO_REQUEST_ENDPOINT,
                                                      RequestType.GET, account.Proxy, headers,
                                                      parameters: null, parametersString: null, parametersContentType: null, referer: null,
                                                      account.UserAgent)
            );
            (string? jsonAnswer, HttpStatusCode responseStatusCode) = taskResult.Result;

            if (responseStatusCode == HttpStatusCode.Unauthorized)
            {
                Log.Error($"GameApiClient GetUserInfo (Account with Id: {account.Id}, Username: {account.Username}) responseStatusCode - Unauthorized!");
                return (ApiResponse.Unauthorized, 0, 0);
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient GetUserInfo (Account with Id: {account.Id}, Username: {account.Username}) JSON answer: {jsonAnswer}");
                return (ApiResponse.Error, 0, 0);
            }

            try
            {
                dynamic json = JObject.Parse(jsonAnswer.Replace("'", "\\'").Replace("\"", "'"));

                string availableBalanceString = json.availableBalance;
                int    playPassesString       = json.playPasses;

                double availableBalance = double.Parse(availableBalanceString.Replace(",", "."));

                return (ApiResponse.Success, availableBalance, playPassesString);
            }
            catch (Exception ex)
            {
                Log.Error($"GameApiClient GetUserInfo (Account with Id: {account.Id}, Username: {account.Username}) Exception: {ex}");
                return (ApiResponse.Error, 0, 0);
            }
        }

        public ApiResponse StartFarming(Account account)
        {
            var headers = GetUniqueHeaders(_commonHeaders, account.AccessToken);

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                await HTTPController.SendRequestAsync(BASE_GAMING_API_URL + START_FARMING_REQUEST_ENDPOINT,
                                                      RequestType.POST, account.Proxy, headers,
                                                      parameters: null, parametersString: null, parametersContentType: null, referer: null,
                                                      account.UserAgent)
            );
            (string? jsonAnswer, HttpStatusCode responseStatusCode) = taskResult.Result;

            if (responseStatusCode == HttpStatusCode.Unauthorized)
            {
                Log.Error($"GameApiClient StartFarming (Account with Id: {account.Id}, Username: {account.Username}) responseStatusCode - Unauthorized!");
                return ApiResponse.Unauthorized;
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient StartFarming (Account with Id: {account.Id}, Username: {account.Username}) JSON answer: {jsonAnswer}");
                return ApiResponse.Error;
            }

            return ApiResponse.Success;
        }

        public (ApiResponse response, double balance, int tickets) ClaimFarming(Account account)
        {
            var headers = GetUniqueHeaders(_commonHeaders, account.AccessToken);

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                await HTTPController.SendRequestAsync(BASE_GAMING_API_URL + CLAIM_FARMING_REQUEST_ENDPOINT,
                                                      RequestType.POST, account.Proxy, headers,
                                                      parameters: null, parametersString: null, parametersContentType: null, referer: null,
                                                      account.UserAgent)
            );
            (string? jsonAnswer, HttpStatusCode responseStatusCode) = taskResult.Result;

            if (responseStatusCode == HttpStatusCode.Unauthorized)
            {
                Log.Error($"GameApiClient ClaimFarming (Account with Id: {account.Id}, Username: {account.Username}) responseStatusCode - Unauthorized!");
                return (ApiResponse.Unauthorized, 0, 0);
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient ClaimFarming (Account with Id: {account.Id}, Username: {account.Username}) JSON answer: {jsonAnswer}");
                return (ApiResponse.Error, 0, 0);
            }

            try
            {
                dynamic json = JObject.Parse(jsonAnswer.Replace("'", "\\'").Replace("\"", "'"));

                string availableBalanceString = json.availableBalance;
                int    playPassesString       = json.playPasses;

                double availableBalance = double.Parse(availableBalanceString.Replace(",", "."));

                return (ApiResponse.Success, availableBalance, playPassesString);
            }
            catch (Exception ex)
            {
                Log.Error($"GameApiClient ClaimFarming (Account with Id: {account.Id}, Username: {account.Username}) Exception: {ex}");
                return (ApiResponse.Error, 0, 0);
            }
        }

        public (ApiResponse response, List<(string id, string kind, string status)> tasks) GetTasks(Account account)
        {
            var headers = GetUniqueHeaders(_commonHeaders, account.AccessToken);

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                await HTTPController.SendRequestAsync(BASE_GAMING_API_URL + TASKS_INFO_REQUEST_ENDPOINT,
                                                      RequestType.GET, account.Proxy, headers,
                                                      parameters: null, parametersString: null, parametersContentType: null, referer: null,
                                                      account.UserAgent)
            );
            (string? jsonAnswer, HttpStatusCode responseStatusCode) = taskResult.Result;

            if (responseStatusCode == HttpStatusCode.Unauthorized)
            {
                Log.Error($"GameApiClient GetTasks (Account with Id: {account.Id}, Username: {account.Username}) responseStatusCode - Unauthorized!");
                return (ApiResponse.Unauthorized, []);
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient GetTasks (Account with Id: {account.Id}, Username: {account.Username}) JSON answer: {jsonAnswer}");
                return (ApiResponse.Error, []);
            }

            try
            {
                dynamic jsonArray = JArray.Parse(jsonAnswer.Replace("'", "\\'").Replace("\"", "'"));

                var tasks = new List<(string id, string kind, string status)>();

                foreach (var task in jsonArray)
                {
                    tasks.Add((task["id"].ToString(), task["kind"].ToString(), task["status"].ToString()));
                }

                return (ApiResponse.Success, tasks);
            }
            catch (Exception ex)
            {
                Log.Error($"GameApiClient GetTasks (Account with Id: {account.Id}, Username: {account.Username}) Exception: {ex}");
                return (ApiResponse.Error, []);
            }
        }

        public ApiResponse StartTask(Account account, string taskId)
        {
            var headers = GetUniqueHeaders(_commonHeaders, account.AccessToken);

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                                     await HTTPController.SendRequestAsync(BASE_GAMING_API_URL + string.Format(START_TASK_REQUEST_ENDPOINT, taskId),
                                                                RequestType.POST, account.Proxy, headers,
                                                                parameters: null, parametersString: null, parametersContentType: null, referer: null,
                                                                account.UserAgent)
                                 );
            (string? jsonAnswer, HttpStatusCode responseStatusCode) = taskResult.Result;

            if (responseStatusCode == HttpStatusCode.Unauthorized)
            {
                Log.Error($"GameApiClient StartTask (Account with Id: {account.Id}, Username: {account.Username}) responseStatusCode - Unauthorized!");
                return ApiResponse.Unauthorized;
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient StartTask (Account with Id: {account.Id}, Username: {account.Username}) JSON answer: {jsonAnswer}");
                return ApiResponse.Error;
            }

            return ApiResponse.Success;
        }

        public (ApiResponse response, double reward) ClaimTask(Account account, string taskId)
        {
            var headers = GetUniqueHeaders(_commonHeaders, account.AccessToken);

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                                     await HTTPController.SendRequestAsync(BASE_GAMING_API_URL + string.Format(CLAIM_TASK_REQUEST_ENDPOINT, taskId),
                                                                RequestType.POST, account.Proxy, headers,
                                                                parameters: null, parametersString: null, parametersContentType: null, referer: null,
                                                                account.UserAgent)
                                 );
            (string? jsonAnswer, HttpStatusCode responseStatusCode) = taskResult.Result;

            if (responseStatusCode == HttpStatusCode.Unauthorized)
            {
                Log.Error($"GameApiClient ClaimTask (Account with Id: {account.Id}, Username: {account.Username}) responseStatusCode - Unauthorized!");
                return (ApiResponse.Unauthorized, 0);
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient ClaimTask (Account with Id: {account.Id}, Username: {account.Username}) JSON answer: {jsonAnswer}");
                return (ApiResponse.Error, 0);
            }

            try
            {
                dynamic json = JObject.Parse(jsonAnswer.Replace("'", "\\'").Replace("\"", "'"));

                string rewardString = json.reward;
                int reward = int.Parse(rewardString);

                return (ApiResponse.Success, reward);
            }
            catch (Exception ex)
            {
                Log.Error($"GameApiClient ClaimTask (Account with Id: {account.Id}, Username: {account.Username}) Exception: {ex}");
                return (ApiResponse.Error, 0);
            }
        }

        public ApiResponse ClaimFriends(Account account)
        {
            var headers = GetUniqueHeaders(_commonHeaders, account.AccessToken);

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                                     await HTTPController.SendRequestAsync(BASE_GATEWAY_API_URL + FRIENDS_CLAIM_REQUEST_ENDPOINT,
                                                                RequestType.POST, account.Proxy, headers,
                                                                parameters: null, parametersString: null, parametersContentType: null, referer: null,
                                                                account.UserAgent)
                                 );
            (string? jsonAnswer, HttpStatusCode responseStatusCode) = taskResult.Result;

            if (responseStatusCode == HttpStatusCode.Unauthorized)
            {
                Log.Error($"GameApiClient ClaimFriends (Account with Id: {account.Id}, Username: {account.Username}) responseStatusCode - Unauthorized!");
                return ApiResponse.Unauthorized;
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient ClaimFriends (Account with Id: {account.Id}, Username: {account.Username}) JSON answer: {jsonAnswer}");
                return ApiResponse.Error;
            }

            return ApiResponse.Success;
        }
    }
}
