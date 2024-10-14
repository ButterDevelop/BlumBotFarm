using BlumBotFarm.Core;
using BlumBotFarm.Core.Models;
using Jering.Javascript.NodeJS;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Net;

namespace BlumBotFarm.GameClient
{
    public class GameApiClient
    {
        private const string BASE_GAMING_API_URL                   = "https://game-domain.blum.codes/api/",
                             BASE_USER_DOMAIN_API_URL              = "https://user-domain.blum.codes/api/",
                             BASE_EARN_DOMAIN_API_URL              = "https://earn-domain.blum.codes/api/",
                             BASE_WALLET_DOMAIN_API_URL            = "https://wallet-domain.blum.codes/api/",
                             HTML_PAGE_URL                         = "https://telegram.blum.codes/",
                             ABOUT_ME_REQUEST_ENDPOINT             = "v1/user/me",
                             GET_AUTH_BY_PROVIDER_REQUEST_ENDPOINT = "v1/auth/provider/PROVIDER_TELEGRAM_MINI_APP",
                             REFRESH_AUTH_REQUEST_ENDPOINT         = "v1/auth/refresh",
                             GET_DAILY_REWARD_REQUEST_ENDPOINT     = "v1/daily-reward?offset=",
                             START_GAME_REQUEST_ENDPOINT           = "v2/game/play",
                             END_GAME_REQUEST_ENDPOINT             = "v2/game/claim",
                             ELIGIBILITY_FOR_DROP_DOGS             = "v2/game/eligibility/dogs_drop",
                             GET_USER_INFO_REQUEST_ENDPOINT        = "v1/user/balance",
                             START_FARMING_REQUEST_ENDPOINT        = "v1/farming/start",
                             CLAIM_FARMING_REQUEST_ENDPOINT        = "v1/farming/claim",
                             TASKS_INFO_REQUEST_ENDPOINT           = "v1/tasks",
                             START_TASK_REQUEST_ENDPOINT           = "v1/tasks/{0}/start",
                             CLAIM_TASK_REQUEST_ENDPOINT           = "v1/tasks/{0}/claim",
                             FRIENDS_BALANCE_REQUEST_ENDPOINT      = "v1/friends/balance",
                             FRIENDS_CLAIM_REQUEST_ENDPOINT        = "v1/friends/claim";

        private readonly string _jsPayloadForDropGame = Properties.Resources.payloadForDropGame;

        private readonly Dictionary<string, string> _commonHeaders = new()
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

        public (ApiResponse response, string blumUsername) GetAboutMeInfo(Account account)
        {
            var headers = GetUniqueHeaders(_commonHeaders, account.AccessToken);

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                await HTTPController.SendRequestAsync(BASE_USER_DOMAIN_API_URL + ABOUT_ME_REQUEST_ENDPOINT,
                                                      RequestType.GET, account.Proxy, headers,
                                                      parameters: null, parametersString: null, parametersContentType: null, referer: null,
                                                      account.UserAgent)
            );
            (string? jsonAnswer, HttpStatusCode responseStatusCode) = taskResult.Result;

            if (responseStatusCode == HttpStatusCode.Unauthorized)
            {
                Log.Error($"GameApiClient GetAboutMeInfo (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) responseStatusCode - Unauthorized!");
                return (ApiResponse.Unauthorized, string.Empty);
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient GetAboutMeInfo (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) JSON answer: {jsonAnswer}");
                return (ApiResponse.Error, string.Empty);
            }

            try
            {
                dynamic json = JObject.Parse(jsonAnswer.Replace("'", "\\'").Replace("\"", "'"));

                string username = json.username;

                return (ApiResponse.Success, username);
            }
            catch (RuntimeBinderException ex)
            {
                Log.Error($"GameApiClient GetAboutMeInfo (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) Exception: {ex}");
                return (ApiResponse.Error, string.Empty);
            }
        }

        public (ApiResponse response, string accessToken, string refreshToken) GetAuthByProvider(Account account)
        {
            var headers = GetUniqueHeaders(_commonHeaders, string.Empty);

            string parametersString      = $"{{\"query\":\"{account.ProviderToken}\"}}";
            string parametersContentType = "application/json";

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                await HTTPController.SendRequestAsync(BASE_USER_DOMAIN_API_URL + GET_AUTH_BY_PROVIDER_REQUEST_ENDPOINT,
                                                      RequestType.POST, account.Proxy, headers,
                                                      parameters: null, parametersString, parametersContentType, referer: null,
                                                      account.UserAgent)
            );
            (string? jsonAnswer, HttpStatusCode responseStatusCode) = taskResult.Result;

            if (responseStatusCode == HttpStatusCode.Unauthorized)
            {
                Log.Error($"GameApiClient GetAuthByProvider (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) responseStatusCode - Unauthorized!");
                return (ApiResponse.Unauthorized, string.Empty, string.Empty);
            }

            if (jsonAnswer != null && jsonAnswer.Contains("signature is invalid"))
            {
                Log.Error($"GameApiClient GetAuthByProvider (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) Signature is invalid! ResponseStatusCode - Unauthorized!");
                return (ApiResponse.Unauthorized, string.Empty, string.Empty);
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient GetAuthByProvider (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) JSON answer: {jsonAnswer}");
                return (ApiResponse.Error, string.Empty, string.Empty);
            }

            try
            {
                dynamic json = JObject.Parse(jsonAnswer.Replace("'", "\\'").Replace("\"", "'"));

                string accessToken  = json.token.access;
                string refreshToken = json.token.refresh;

                return (ApiResponse.Success, accessToken, refreshToken);
            }
            catch (RuntimeBinderException ex)
            {
                Log.Error($"GameApiClient GetAuthByProvider (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) Exception: {ex}");
                return (ApiResponse.Error, string.Empty, string.Empty);
            }
        }

        public (ApiResponse response, string accessToken, string refreshToken) RefreshAuth(Account account)
        {
            var headers = GetUniqueHeaders(_commonHeaders, string.Empty);

            string parametersString      = $"{{\"refresh\":\"{account.RefreshToken}\"}}";
            string parametersContentType = "application/json";

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                await HTTPController.SendRequestAsync(BASE_USER_DOMAIN_API_URL + REFRESH_AUTH_REQUEST_ENDPOINT,
                                                      RequestType.POST, account.Proxy, headers,
                                                      parameters: null, parametersString, parametersContentType, referer: null,
                                                      account.UserAgent)
            );
            (string? jsonAnswer, HttpStatusCode responseStatusCode) = taskResult.Result;

            if (responseStatusCode == HttpStatusCode.Unauthorized)
            {
                Log.Error($"GameApiClient RefreshAuth (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) responseStatusCode - Unauthorized!");
                return (ApiResponse.Unauthorized, string.Empty, string.Empty);
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient RefreshAuth (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) JSON answer: {jsonAnswer}");
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
                Log.Error($"GameApiClient RefreshAuth (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) Exception: {ex}");
                return (ApiResponse.Error, string.Empty, string.Empty);
            }
        }

        public (ApiResponse response, bool sameDay) GetDailyReward(Account account)
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
                Log.Error($"GameApiClient GetDailyReward (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) responseStatusCode - Unauthorized!");
                return (ApiResponse.Unauthorized, false);
            }

            if (jsonAnswer != null && jsonAnswer.Contains("same day", StringComparison.CurrentCultureIgnoreCase))
            {
                Log.Information($"GameApiClient GetDailyReward (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}). Same day! JSON answer: {jsonAnswer}");
                return (ApiResponse.Error, true);
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient GetDailyReward (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) JSON answer: {jsonAnswer}");
                return (ApiResponse.Error, false);
            }

            return (ApiResponse.Success, false);
        }

        public (ApiResponse response, bool eligible) EligibleForDogsDrop(Account account)
        {
            var headers = GetUniqueHeaders(_commonHeaders, account.AccessToken);

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                await HTTPController.SendRequestAsync(BASE_GAMING_API_URL + ELIGIBILITY_FOR_DROP_DOGS,
                                                      RequestType.GET, account.Proxy, headers,
                                                      parameters: null, parametersString: null, parametersContentType: null, referer: null,
                                                      account.UserAgent)
            );
            (string? jsonAnswer, HttpStatusCode responseStatusCode) = taskResult.Result;

            if (responseStatusCode == HttpStatusCode.Unauthorized)
            {
                Log.Error($"GameApiClient EligibleForDrop (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) responseStatusCode - Unauthorized!");
                return (ApiResponse.Unauthorized, false);
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient EligibleForDrop (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) JSON answer: {jsonAnswer}");
                return (ApiResponse.Error, false);
            }

            try
            {
                dynamic json = JObject.Parse(jsonAnswer.Replace("'", "\\'").Replace("\"", "'"));

                bool eligible = json.eligible;

                return (ApiResponse.Success, eligible);
            }
            catch (RuntimeBinderException ex)
            {
                Log.Error($"GameApiClient EligibleForDrop (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) Exception: {ex}");
                return (ApiResponse.Error, false);
            }
        }

        public (ApiResponse response, string gameId, Dictionary<string, (int perClick, double probability)> pieces) StartGame(Account account)
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
                Log.Error($"GameApiClient StartGame (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) responseStatusCode - Unauthorized!");
                return (ApiResponse.Unauthorized, string.Empty, []);
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient StartGame (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) JSON answer: {jsonAnswer}");
                return (ApiResponse.Error, string.Empty, []);
            }

            try
            {
                dynamic json = JObject.Parse(jsonAnswer.Replace("'", "\\'").Replace("\"", "'"));
                
                string gameId = json.gameId;

                Dictionary<string, (int perClick, double probability)> resultsPieces = [];

                string[] piecesNames = ["BOMB", "CLOVER", "FREEZE", "DOGS"];
                if (json["assets"] != null)
                {
                    foreach (var pieceName in piecesNames)
                    {
                        if (json["assets"][pieceName] != null)
                        {
                            try
                            {
                                string perClickString    = json["assets"][pieceName]["perClick"];
                                string probabilityString = json["assets"][pieceName]["probability"];

                                var perClick    = int.Parse(perClickString);
                                var probability = double.Parse(probabilityString.Replace(",", "."));

                                resultsPieces.Add(pieceName, (perClick, probability));
                            }
                            catch { }
                        }
                    }
                }

                return (ApiResponse.Success, gameId, resultsPieces);
            }
            catch (RuntimeBinderException ex)
            {
                Log.Error($"GameApiClient StartGame (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) Exception: {ex}");
                return (ApiResponse.Error, string.Empty, []);
            }
        }

        public ApiResponse EndGame(Account account, string gameId, int points, int dogsPoints)
        {
            var headers = GetUniqueHeaders(_commonHeaders, account.AccessToken);

            Task<string?> result = StaticNodeJSService.InvokeFromStringAsync<string>(
                _jsPayloadForDropGame,
                cacheIdentifier: "jsPayloadForDropGame",
                args: [gameId, points, account.IsEligibleForDogsDrop, dogsPoints]
            );

            string payload = result.Result ?? "";

            string parametersString      = $"{{\"payload\":\"{payload}\"}}";
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
                Log.Error($"GameApiClient EndGame (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) responseStatusCode - Unauthorized!");
                return ApiResponse.Unauthorized;
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient EndGame (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) JSON answer: {jsonAnswer}");
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
                Log.Error($"GameApiClient GetUserInfo (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) responseStatusCode - Unauthorized!");
                return (ApiResponse.Unauthorized, 0, 0);
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient GetUserInfo (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) JSON answer: {jsonAnswer}");
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
                Log.Error($"GameApiClient GetUserInfo (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) Exception: {ex}");
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
                Log.Error($"GameApiClient StartFarming (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) responseStatusCode - Unauthorized!");
                return ApiResponse.Unauthorized;
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient StartFarming (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) JSON answer: {jsonAnswer}");
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
                Log.Error($"GameApiClient ClaimFarming (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) responseStatusCode - Unauthorized!");
                return (ApiResponse.Unauthorized, 0, 0);
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient ClaimFarming (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) JSON answer: {jsonAnswer}");
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
                Log.Error($"GameApiClient ClaimFarming (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) Exception: {ex}");
                return (ApiResponse.Error, 0, 0);
            }
        }

        public (ApiResponse response, List<(string id, string kind, string status, string validationType)> tasks) GetTasks(Account account)
        {
            var headers = GetUniqueHeaders(_commonHeaders, account.AccessToken);

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                await HTTPController.SendRequestAsync(BASE_EARN_DOMAIN_API_URL + TASKS_INFO_REQUEST_ENDPOINT,
                                                      RequestType.GET, account.Proxy, headers,
                                                      parameters: null, parametersString: null, parametersContentType: null, referer: null,
                                                      account.UserAgent)
            );
            (string? jsonAnswer, HttpStatusCode responseStatusCode) = taskResult.Result;

            if (responseStatusCode == HttpStatusCode.Unauthorized)
            {
                Log.Error($"GameApiClient GetTasks (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) responseStatusCode - Unauthorized!");
                return (ApiResponse.Unauthorized, []);
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient GetTasks (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) JSON answer: {jsonAnswer}");
                return (ApiResponse.Error, []);
            }

            try
            {
                dynamic jsonArray = JArray.Parse(jsonAnswer.Replace("'", "\\'").Replace("\"", "'"));

                var tasks = new List<(string id, string kind, string status, string validationType)>();

                foreach (var section in jsonArray)
                {
                    if (section["tasks"] != null)
                    {
                        foreach (var task in section.tasks)
                        {
                            if (task["subTasks"] is null)
                            {
                                tasks.Add((task["id"].ToString(), task["kind"].ToString(), task["status"].ToString(), task["validationType"].ToString()));
                            }
                            else
                            {
                                foreach (var subTask in task["subTasks"])
                                {
                                    tasks.Add((subTask["id"].ToString(), subTask["kind"].ToString(), subTask["status"].ToString(), subTask["validationType"].ToString()));
                                }
                            }
                        }
                    }

                    if (section["subSections"] != null)
                    {
                        foreach (var subSection in section.subSections)
                        {
                            foreach (var task in subSection.tasks)
                            {
                                if (task["subTasks"] is null)
                                {
                                    tasks.Add((task["id"].ToString(), task["kind"].ToString(), task["status"].ToString(), task["validationType"].ToString()));
                                }
                                else
                                {
                                    foreach (var subTask in task["subTasks"])
                                    {
                                        tasks.Add((subTask["id"].ToString(), subTask["kind"].ToString(), subTask["status"].ToString(), subTask["validationType"].ToString()));
                                    }
                                }
                            }
                        }
                    }
                }

                return (ApiResponse.Success, tasks);
            }
            catch (Exception ex)
            {
                Log.Error($"GameApiClient GetTasks (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) Exception: {ex}");
                return (ApiResponse.Error, []);
            }
        }

        public ApiResponse StartTask(Account account, string taskId)
        {
            var headers = GetUniqueHeaders(_commonHeaders, account.AccessToken);

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                                     await HTTPController.SendRequestAsync(BASE_EARN_DOMAIN_API_URL + string.Format(START_TASK_REQUEST_ENDPOINT, taskId),
                                                                RequestType.POST, account.Proxy, headers,
                                                                parameters: null, parametersString: null, parametersContentType: null, referer: null,
                                                                account.UserAgent)
                                 );
            (string? jsonAnswer, HttpStatusCode responseStatusCode) = taskResult.Result;

            if (responseStatusCode == HttpStatusCode.Unauthorized)
            {
                Log.Error($"GameApiClient StartTask (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) responseStatusCode - Unauthorized!");
                return ApiResponse.Unauthorized;
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient StartTask (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) JSON answer: {jsonAnswer}");
                return ApiResponse.Error;
            }

            return ApiResponse.Success;
        }

        public (ApiResponse response, double reward) ClaimTask(Account account, string taskId)
        {
            var headers = GetUniqueHeaders(_commonHeaders, account.AccessToken);

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                                     await HTTPController.SendRequestAsync(BASE_EARN_DOMAIN_API_URL + string.Format(CLAIM_TASK_REQUEST_ENDPOINT, taskId),
                                                                RequestType.POST, account.Proxy, headers,
                                                                parameters: null, parametersString: null, parametersContentType: null, referer: null,
                                                                account.UserAgent)
                                 );
            (string? jsonAnswer, HttpStatusCode responseStatusCode) = taskResult.Result;

            if (responseStatusCode == HttpStatusCode.Unauthorized)
            {
                Log.Error($"GameApiClient ClaimTask (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) responseStatusCode - Unauthorized!");
                return (ApiResponse.Unauthorized, 0);
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient ClaimTask (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) JSON answer: {jsonAnswer}");
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
                Log.Error($"GameApiClient ClaimTask (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) Exception: {ex}");
                return (ApiResponse.Error, 0);
            }
        }

        public (ApiResponse response, bool canClaim, string referralToken, int referralsCount) FriendsBalance(Account account)
        {
            var headers = GetUniqueHeaders(_commonHeaders, account.AccessToken);

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                                     await HTTPController.SendRequestAsync(BASE_USER_DOMAIN_API_URL + FRIENDS_BALANCE_REQUEST_ENDPOINT,
                                                                RequestType.GET, account.Proxy, headers,
                                                                parameters: null, parametersString: null, parametersContentType: null, referer: null,
                                                                account.UserAgent)
                                 );
            (string? jsonAnswer, HttpStatusCode responseStatusCode) = taskResult.Result;

            if (responseStatusCode == HttpStatusCode.Unauthorized)
            {
                Log.Error($"GameApiClient FriendsBalance (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) responseStatusCode - Unauthorized!");
                return (ApiResponse.Unauthorized, false, string.Empty, 0);
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient FriendsBalance (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) JSON answer: {jsonAnswer}");
                return (ApiResponse.Error, false, string.Empty, 0);
            }

            try
            {
                dynamic json = JObject.Parse(jsonAnswer.Replace("'", "\\'").Replace("\"", "'"));

                bool   canClaim             = json.canClaim;
                string referralToken        = json.referralToken;
                string referralsCountString = json.usedInvitation;

                int  referralsCount = int.Parse(referralsCountString);

                return (ApiResponse.Success, canClaim, referralToken, referralsCount);
            }
            catch (Exception ex)
            {
                Log.Error($"GameApiClient FriendsBalance (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) Exception: {ex}, JSON answer: {jsonAnswer}");
                return (ApiResponse.Error, false, string.Empty, 0);
            }
        }

        public ApiResponse ClaimFriends(Account account)
        {
            var headers = GetUniqueHeaders(_commonHeaders, account.AccessToken);

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                                     await HTTPController.SendRequestAsync(BASE_USER_DOMAIN_API_URL + FRIENDS_CLAIM_REQUEST_ENDPOINT,
                                                                RequestType.POST, account.Proxy, headers,
                                                                parameters: null, parametersString: null, parametersContentType: null, referer: null,
                                                                account.UserAgent)
                                 );
            (string? jsonAnswer, HttpStatusCode responseStatusCode) = taskResult.Result;

            if (responseStatusCode == HttpStatusCode.Unauthorized)
            {
                Log.Error($"GameApiClient ClaimFriends (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) responseStatusCode - Unauthorized!");
                return ApiResponse.Unauthorized;
            }

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"GameApiClient ClaimFriends (Account with Id: {account.Id}, CustomUsername: {account.CustomUsername}, BlumUsername: {account.BlumUsername}) JSON answer: {jsonAnswer}");
                return ApiResponse.Error;
            }

            return ApiResponse.Success;
        }
    }
}
