using BlumBotFarm.Core;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Net;

namespace AutoBlumFarmServer.Helpers
{
    public class ProxySellerAPIHelper
    {
        // BASE_URL + _apiToken + ENDPOINT
        private const string BASE_URL                 = "https://proxy-seller.com/personal/api/v1/",
                             ADD_RESIDENT_ENDPOINT    = "/resident/list/add",
                             DELETE_RESIDENT_ENDPOINT = "/resident/list/delete",
                             DOWNLOAD_PROXY_LIST      = "/proxy/download/resident";

        private string _apiToken;

        public ProxySellerAPIHelper(string apiToken)
        {
            _apiToken = apiToken;
        }

        public (bool result, int listId) AddResident(int userId, int accountId, string countryCode)
        {
            string listName              = $"API UserId={userId} AccountId={accountId} {countryCode}";
            string parametersString      = $"{{\"title\":\"{listName}\",\"geo\":{{\"country\":\"{countryCode}\"}},\"export\":{{\"ports\":1,\"ext\":\"txt\"}}}}";
            string parametersContentType = "application/json";

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                await HTTPController.SendRequestAsync(BASE_URL + _apiToken + ADD_RESIDENT_ENDPOINT,
                                                      RequestType.POST, parametersString: parametersString, parametersContentType: parametersContentType)
            );

            (string? jsonAnswer, HttpStatusCode responseStatusCode) = taskResult.Result;
            
            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"ProxySellerAPIHelper AddResident (User Id: {userId}, Account Id: {accountId}, Country Code: {countryCode}) JSON answer: {jsonAnswer}");
                return (false, 0);
            }

            Log.Information($"ProxySellerAPIHelper AddResident (User Id: {userId}, Account Id: {accountId}, Country Code: {countryCode}) Seems good. JSON answer: {jsonAnswer}");
            try
            {
                dynamic json = JObject.Parse(jsonAnswer.Replace("'", "\\'").Replace("\"", "'"));

                string status = json.status;
                int listId    = json.data.id;
                string title  = json.data.title;

                return (status == "success" && title == listName, listId);
            }
            catch (RuntimeBinderException ex)
            {
                Log.Error($"ProxySellerAPIHelper AddResident (User Id: {userId}, Account Id: {accountId}, Country Code: {countryCode}) Exception: {ex}");
                return (false, 0);
            }
        }

        public bool DeleteResident(int userId, int accountId, int listId)
        {
            Dictionary<string, string> parameters = new()
            {
                { "id", listId.ToString() }
            };

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                await HTTPController.SendRequestAsync(BASE_URL + _apiToken + DELETE_RESIDENT_ENDPOINT,
                                                      RequestType.DELETE, parameters: parameters)
            );

            (string? jsonAnswer, HttpStatusCode responseStatusCode) = taskResult.Result;

            if (jsonAnswer == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"ProxySellerAPIHelper DeleteResident (User Id: {userId}, Account Id: {accountId}, List ID: {listId}) JSON answer: {jsonAnswer}");
                return false;
            }

            Log.Information($"ProxySellerAPIHelper DeleteResident (User Id: {userId}, Account Id: {accountId}, List ID: {listId}) Seems good. JSON answer: {jsonAnswer}");
            try
            {
                dynamic json = JObject.Parse(jsonAnswer.Replace("'", "\\'").Replace("\"", "'"));
                string status = json.status;   
                return status == "success";
            }
            catch (RuntimeBinderException ex)
            {
                Log.Error($"ProxySellerAPIHelper DeleteResident (User Id: {userId}, Account Id: {accountId}, List ID: {listId}) Exception: {ex}");
                return false;
            }
        }

        public (bool result, string content) DownloadFile(int userId, int accountId, int listId)
        {
            Dictionary<string, string> parameters = new()
            {
                { "listId", listId.ToString() }
            };

            var taskResult = HTTPController.ExecuteFunctionUntilSuccessAsync(async () =>
                await HTTPController.SendRequestAsync(BASE_URL + _apiToken + DOWNLOAD_PROXY_LIST,
                                                      RequestType.GET, parameters: parameters)
            );

            (string? content, HttpStatusCode responseStatusCode) = taskResult.Result;

            if (content == null || responseStatusCode != HttpStatusCode.OK)
            {
                Log.Error($"ProxySellerAPIHelper DownloadFile (User Id: {userId}, Account Id: {accountId}, List ID: {listId}) Answer: {content}");
                return (false, string.Empty);
            }

            return (true, content);
        }
    }
}
