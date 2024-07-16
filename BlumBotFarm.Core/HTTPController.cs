using Serilog;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Yove.Proxy;

namespace BlumBotFarm.Core
{
    public enum RequestType
    {
        GET    = 0,
        POST   = 1,
        DELETE = 2
    }

    public class HTTPController
    {
        public const int COUNT_OF_REQUEST_ATTEMPTS = 3;

        private static string[] _userAgents = [];
        private static Random   _rnd        = new();

        private static ConcurrentDictionary<string, (HttpClient client, DateTime lastUsed)> _savedHttpClients = new();
        // время жизни клиента, то есть стандартного соединения keep-alive, по умолчанию это 20 минут
        private static readonly TimeSpan ClientLifetime = TimeSpan.FromMinutes(20);

        private static bool ServerCertificateValidationCallback(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public static void Initialize(string uas)
        {
            _userAgents = uas.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            _rnd = new Random();
        }

        public static string GetRandomUserAgent()
        {
            return _userAgents[_rnd.Next(_userAgents.Length)];
        }

        private static HttpClient GenerateClient(string proxy, int connectTimeoutMs)
        {
            HttpClientHandler handler = new()
            {
                ServerCertificateCustomValidationCallback = ServerCertificateValidationCallback
            };

            if (!string.IsNullOrEmpty(proxy))
            {
                string? proxyAddress;
                string? username = null;
                string? password = null;

                ProxyType proxyType = proxy.Contains("socks5") ? ProxyType.Socks5 : (proxy.Contains("socks4") ? ProxyType.Socks4 : ProxyType.Http);
                //ProxyClient? proxyYove = null;
                WebProxy proxyWeb;

                // Проверяем наличие логина и пароля в строке прокси
                if (proxy.Contains('@'))
                {
                    // Пример: protocol://ip:port@login:password
                    var protocolSplit = proxy.Split(new[] { "://" }, 2, StringSplitOptions.None);
                    var protocol = protocolSplit[0];
                    var rest     = protocolSplit[1];

                    var atIndex     = rest.LastIndexOf('@');
                    var addressPart = rest[..atIndex];
                    var loginPart   = rest[(atIndex + 1)..];

                    var addressParts = addressPart.Split(':');
                    var ip   = addressParts[0];
                    var port = addressParts.Length > 1 ? addressParts[1] : string.Empty;

                    var loginParts = loginPart.Split(':');
                    username = loginParts.Length > 1 ? loginParts[0] : null;
                    password = loginParts.Length > 1 ? loginParts[1] : null;

                    proxyAddress = $"{ip}:{port}";

                    if (username != null && password != null)
                    {
                        //proxyYove = new ProxyClient(proxyAddress, username, password, proxyType);

                        ICredentials credentials = new NetworkCredential(username, password);
                        proxyWeb = new WebProxy(proxyAddress, true, null, credentials);
                    }
                    else
                    {
                        //proxyYove = new ProxyClient(proxyAddress, proxyType);
                        proxyWeb = new WebProxy(proxyAddress);
                    }
                }
                else
                {
                    proxyAddress = proxy.Replace("socks5://", "").Replace("socks4://", "").Replace("http://", "");

                    proxyWeb = new WebProxy(proxyAddress);
                    //proxyYove = new ProxyClient(proxyAddress, proxyType);
                }

                //handler.Proxy    = proxyYove;
                handler.Proxy = proxyWeb;
                handler.UseProxy = true;
            }

            HttpClient client = new(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(connectTimeoutMs)
            };
            return client;
        }

        public static async Task<(string? answer, HttpStatusCode responseStatusCode)> SendRequestAsync(string url, RequestType type, string proxy = "",
                                 Dictionary<string, string>? headers = null, Dictionary<string, string>? parameters = null,
                                 string? parametersString = null, string? parametersContentType = null,
                                 string? referer = null, string? userAgent = null, int connectTimeoutMs = 10000)
        {
            try
            {
                string keyDict = $"{proxy}";
                HttpClient client;
                DateTime lastUsed;

                lock (_savedHttpClients)
                {
                    if (_savedHttpClients.TryGetValue(keyDict, out var clientEntry))
                    {
                        (client, lastUsed) = clientEntry;
                        if (DateTime.Now - lastUsed > ClientLifetime)
                        {
                            client.Dispose();
                            client = GenerateClient(proxy, connectTimeoutMs);
                            _savedHttpClients[keyDict] = (client, DateTime.Now);
                        }
                    }
                    else
                    {
                        client = GenerateClient(proxy, connectTimeoutMs);
                        _savedHttpClients[keyDict] = (client, DateTime.Now);
                    }
                }

                client.DefaultRequestHeaders.Clear();

                if (!string.IsNullOrEmpty(referer))
                {
                    client.DefaultRequestHeaders.Referrer = new Uri(referer);
                }

                client.DefaultRequestHeaders.Connection.Add("keep-alive");
                client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent ?? GetRandomUserAgent());

                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                HttpResponseMessage? response = null;
                if (type == RequestType.GET)
                {
                    var requestUri = parameters != null ? QueryHelpers.AddQueryString(url, parameters) : url;
                    response = await client.GetAsync(requestUri);
                }
                else if (type == RequestType.DELETE)
                {
                    var requestUri = parameters != null ? QueryHelpers.AddQueryString(url, parameters) : url;
                    response = await client.DeleteAsync(requestUri);
                }
                else if (type == RequestType.POST)
                {
                    if (parameters != null)
                    {
                        var content = new FormUrlEncodedContent(parameters);
                        response = await client.PostAsync(url, content);
                    }
                    else if (!string.IsNullOrEmpty(parametersString) && !string.IsNullOrEmpty(parametersContentType))
                    {
                        var content = new StringContent(parametersString, Encoding.UTF8, parametersContentType);
                        response = await client.PostAsync(url, content);
                    }
                    else
                    {
                        response = await client.PostAsync(url, null);
                    }
                }

                if (response == null) throw new Exception("Response is NULL!");

                return (await response.Content.ReadAsStringAsync(), response.StatusCode);
            }
            catch (Exception ex)
            {
                Log.Error($"HTTPController SendRequestAsync. Proxy: {proxy}, Exception: {ex.Message}");
                return (null, HttpStatusCode.ServiceUnavailable);
            }
        }

        public static async Task<string?> DownloadImageBase64FromURLAsync(string imageUrl, string proxy, int connectTimeoutMs = 5000)
        {
            try
            {
                var client = GenerateClient(proxy, connectTimeoutMs);

                client.Timeout = TimeSpan.FromMilliseconds(connectTimeoutMs);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(GetRandomUserAgent());

                var response = await client.GetAsync(imageUrl);

                if (response.IsSuccessStatusCode)
                {
                    byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

                    // Преобразуем массив байтов в строку Base64
                    string base64Image = Convert.ToBase64String(imageBytes);
                    return base64Image;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        public static async Task<(string? answer, HttpStatusCode responseStatusCode)> ExecuteFunctionUntilSuccessAsync(
            Func<Task<(string?, HttpStatusCode)>> function, int countOfAttempts = COUNT_OF_REQUEST_ATTEMPTS)
        {
            (string? answer, HttpStatusCode responseStatusCode) result = (null, HttpStatusCode.ServiceUnavailable);
            int attempts = 0;
            while (attempts++ < countOfAttempts)
            {
                result = await function();
                if (result.answer != null) break;
            }
            return result;
        }
    }

    public static class QueryHelpers
    {
        public static string AddQueryString(string uri, IDictionary<string, string> parameters)
        {
            var builder = new UriBuilder(uri);
            var query = new StringBuilder();
            foreach (var parameter in parameters)
            {
                if (query.Length > 0)
                {
                    query.Append('&');
                }
                query.AppendFormat("{0}={1}", WebUtility.UrlEncode(parameter.Key), WebUtility.UrlEncode(parameter.Value));
            }
            builder.Query = query.ToString();
            return builder.ToString();
        }
    }
}
