using Serilog;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using xNet;

namespace BlumBotFarm.GameClient
{
    public enum RequestType
    {
        GET  = 0,
        POST = 1
    }

    public class HTTPController
    {
        public const int COUNT_OF_REQUEST_ATTEMPTS = 3;

        private static string[] _userAgents = Array.Empty<string>();
        private static Random   _rnd        = new();

        public static void Initialize(string uas)
        {
            _userAgents = uas.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            _rnd = new Random();
        }

        public static string GetRandomUserAgent()
        {
            return _userAgents[_rnd.Next(_userAgents.Length)];
        }

        public static bool ServerCertificateValidationCallback(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public static string? SendRequest(string url, out HttpStatusCode responseStatusCode, RequestType type, string proxy = "",
                                         Dictionary<string, string>? headers = null, Dictionary<string, string>? parameters = null,
                                         string? parametersString = null, string? parametersContentType = null,
                                         string? referer = null, string? userAgent = null, int connectTimeoutMs = 1000)
        {
            try
            {
                string html = "";
                using (var request = new HttpRequest())
                {
                    request.SslCertificateValidatorCallback += ServerCertificateValidationCallback;
                    request.IgnoreProtocolErrors = true;
                    request.ConnectTimeout = connectTimeoutMs;

                    if (referer != null) request.Referer = referer;

                    request.UserAgent = userAgent ?? GetRandomUserAgent();
                    request.KeepAlive = true;

                    if (!string.IsNullOrEmpty(proxy))
                    {
                        if (proxy.StartsWith("socks4"))
                        {
                            request.Proxy = Socks5ProxyClient.Parse(proxy.Replace("socks4://", ""));
                        }
                        else
                        if (proxy.StartsWith("socks5"))
                        {
                            request.Proxy = Socks5ProxyClient.Parse(proxy.Replace("socks5://", ""));
                        }
                        else
                        if (proxy.StartsWith("http"))
                        {
                            request.Proxy = HttpProxyClient.Parse(proxy.Replace("http://", "").Replace("https://", ""));
                        }
                    }

                    if (headers != null)
                    {
                        foreach (var header in headers)
                        {
                            request.AddHeader(header.Key, header.Value);
                        }
                    }

                    RequestParams? reqParams = null;
                    if (parameters != null)
                    {
                        reqParams = new RequestParams();
                        foreach (var param in parameters)
                        {
                            reqParams[param.Key] = param.Value;
                        }
                    }

                    HttpResponse? response = null;
                    if (type == RequestType.GET)
                    {
                        response = request.Get(url, reqParams);
                    }
                    else
                    {
                        if (parameters != null)
                        {
                            response = request.Post(url, reqParams);
                        }
                        else
                        if (parametersString != null && !string.IsNullOrEmpty(parametersContentType))
                        {
                            response = request.Post(url, parametersString, parametersContentType);
                        }
                        else
                        {
                            response = request.Post(url);
                        }
                    }

                    responseStatusCode = response.StatusCode;

                    html = response.ToString();
                    return html;
                }
            }
            catch (Exception ex)
            {
                responseStatusCode = HttpStatusCode.None;
                Log.Error("{HTTPController SendRequest} Error: " + ex.ToString());
                return null;
            }
        }

        public static string? DownloadImageBase64FromURL(string imageUrl, int connectTimeoutMs = 1000)
        {
            try
            {
                using (HttpRequest request = new HttpRequest())
                {
                    request.SslCertificateValidatorCallback += ServerCertificateValidationCallback;
                    request.IgnoreProtocolErrors = true;
                    request.ConnectTimeout = connectTimeoutMs;
                    request.UserAgent = GetRandomUserAgent();

                    HttpResponse response = request.Get(imageUrl);

                    if (response.IsOK)
                    {
                        byte[] imageBytes = response.ToBytes();

                        // Преобразуем массив байтов в строку Base64
                        string base64Image = Convert.ToBase64String(imageBytes);
                        return base64Image;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public static string? ExecuteFunctionUntilSuccess(Func<string?> function, int countOfAttempts = COUNT_OF_REQUEST_ATTEMPTS)
        {
            string? result = null;
            int attempts = 0;
            while (attempts++ < countOfAttempts)
            {
                result = function();
                if (result != null) break;
            }
            return result;
        }
    }
}
