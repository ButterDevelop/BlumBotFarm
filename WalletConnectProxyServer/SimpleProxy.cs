using BlumBotFarm.Core;
using BlumBotFarm.Core.Models;
using BlumBotFarm.Database.Repositories;
using BlumBotFarm.GameClient;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;
using Yove.Proxy;
using Task = System.Threading.Tasks.Task;

namespace WalletConnectProxyServer
{
    public class SimpleProxy
    {
        private static readonly string[] WHITE_LIST_HOSTS = ["blum.codes", "bridge"]; // "2ip", "ipify"];
        private static readonly string[] BLACK_LIST_HOSTS = ["posthog", "sentry", "tganalytics"];

        private readonly AccountRepository _accountRepository;
        private readonly ProxyServer       _proxyServer;

        private readonly X509Certificate2 _rootCert;
        private ExplicitProxyEndPoint?    _explicitEndPoint;
        private int                       _currentAccountId;
        private int                       _proxyPort;
        private HttpClient?               _httpClient;
        private Account                   _account;

        private GameApiClient             _gameApiClient;

        private bool                      _isWorkingNow;

        private readonly ConcurrentDictionary<string, X509Certificate2> _certificates;

        public SimpleProxy(int port)
        {
            _rootCert         = GetRootCertificate();
            _proxyPort        = port;
            _currentAccountId = 1;

            _account = new();

            var dbConnectionString = AppConfig.DatabaseSettings.MONGO_CONNECTION_STRING;
            var databaseName       = AppConfig.DatabaseSettings.MONGO_DATABASE_NAME;
            _accountRepository     = new AccountRepository(dbConnectionString, databaseName, AppConfig.DatabaseSettings.MONGO_ACCOUNT_PATH);

            _certificates = [];

            _gameApiClient = new();

            _isWorkingNow = false;

            // Инициализация ProxyServer из Titanium.Web.Proxy
            _proxyServer = new();
        }

        // Метод для запуска прокси-сервера
        public void Start()
        {
            if (_isWorkingNow)
            {
                Debug.WriteLine($"Proxy server is already started.");
                return;
            }
            _isWorkingNow = true;

            _explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Parse("127.0.0.1"), _proxyPort);

            _explicitEndPoint.BeforeTunnelConnectRequest += OnBeforeTunnelConnectRequest;
            _proxyServer.AddEndPoint(_explicitEndPoint);

            _proxyServer.BeforeRequest  += OnRequestCaptureTraffic;
            _proxyServer.BeforeResponse += OnResponseCaptureTraffic;
            _proxyServer.ServerCertificateValidationCallback += OnCertificateValidation;
            _proxyServer.ClientCertificateSelectionCallback  += OnCertificateSelection;

            _proxyServer.Start();

            _proxyServer.SetAsSystemHttpProxy(_explicitEndPoint);
            _proxyServer.SetAsSystemHttpsProxy(_explicitEndPoint);

            Debug.WriteLine($"Proxy server running on port {_proxyPort}...");
        }

        // Метод для остановки прокси-сервера
        public void Stop()
        {
            if (!_isWorkingNow)
            {
                Debug.WriteLine($"Proxy server is already stopped.");
                return;
            }
            _isWorkingNow = false;

            if (_explicitEndPoint != null)
            {
                _explicitEndPoint.BeforeTunnelConnectRequest -= OnBeforeTunnelConnectRequest;
            }
            _proxyServer.ServerCertificateValidationCallback -= OnCertificateValidation;
            _proxyServer.ClientCertificateSelectionCallback  -= OnCertificateSelection;
            _proxyServer.BeforeRequest  -= OnRequestCaptureTraffic;
            _proxyServer.BeforeResponse -= OnResponseCaptureTraffic;

            _proxyServer.Stop();
            
            _proxyServer.DisableSystemHttpProxy();
            _proxyServer.DisableSystemHttpsProxy();

            Debug.WriteLine($"Proxy server stopped.");
        }

        public void ChangeAccountId(int id)
        {
            _currentAccountId = id;

            // Получаем аккаунт
            var account = _accountRepository.GetById(_currentAccountId);
            if (account == null || string.IsNullOrEmpty(account.Proxy) || string.IsNullOrEmpty(account.AccessToken))
            {
                throw new Exception("Unknown account or missing proxy configuration or missing access token.");
            }
            else
            {
                Stop();

                Task.Run(() =>
                {
                    // Обновляем аутентификацию, если нужно
                    if (GameApiUtilsService.AuthCheck(account, _accountRepository, _gameApiClient) == ApiResponse.Unauthorized)
                    {
                        MessageBox.Show("No AUTH for this account!!!", "Wallet Connect Proxy Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        account = _accountRepository.GetById(account.Id);

                        if (account == null)
                        {
                            throw new Exception("Account is NULL after reauth and getting it from DB!");
                        }
                        else
                        {
                            _account = account;
                        }
                    }
                });

                Thread.Sleep(10000);

                Start();
            }
            _account = _accountRepository.GetById(account.Id) ?? _account;

            // Генерируем HttpClient (нужно для перехвата ответа через внешний прокси, если используется)
            _httpClient = GenerateClient(account.Proxy);

            // Если не используется HttpClient (значит, не надо перехватывать и менять ответ сервера), то используем встроенный UpstreamProxy
            try
            {
                Regex proxyRegex = new("(.*)://(.*):(.*)@(.*):(.*)");
                var match = proxyRegex.Match(account.Proxy);
                if (match.Success)
                {
                    ExternalProxyType protocol = ExternalProxyType.Http;
                    switch (match.Groups[1].Value)
                    {
                        case "socks4": protocol = ExternalProxyType.Socks4; break;
                        case "socks5": protocol = ExternalProxyType.Socks5; break;
                    }

                    var proxy = new ExternalProxy
                    {
                        HostName  = match.Groups[2].Value,
                        Port      = int.Parse(match.Groups[3].Value),
                        UserName  = match.Groups[4].Value,
                        Password  = match.Groups[5].Value,
                        ProxyType = protocol
                    };

                    _proxyServer.UpStreamHttpProxy  = proxy;
                    _proxyServer.UpStreamHttpsProxy = proxy;
                }
            }
            catch { }

            Debug.WriteLine($"Changed account id to {_currentAccountId}.");
        }

        private async Task OnRequestCaptureTraffic(object sender, SessionEventArgs e)
        {
            string url = e.HttpClient.Request.Url;

            // Проверка на белый список хостов
            if (!WHITE_LIST_HOSTS.Any(url.Contains) || _httpClient == null) return;

            Debug.WriteLine($"Captured request: {url}");

            // Пример: обработка заголовков для обхода CORS
            e.HttpClient.Request.Headers.RemoveHeader("Access-Control-Allow-Origin");
            e.HttpClient.Request.Headers.RemoveHeader("Access-Control-Allow-Methods");
            e.HttpClient.Request.Headers.AddHeader("Access-Control-Allow-Origin", "*");
            e.HttpClient.Request.Headers.AddHeader("Access-Control-Allow-Methods", "GET,POST,PUT,DELETE,OPTIONS");

            // Меняем заголовок Authorization, если он есть
            string headerName = "Authorization";
            if (e.HttpClient.Request.Headers.Any(h => h.Name == headerName))
            {
                e.HttpClient.Request.Headers.RemoveHeader(headerName);
                e.HttpClient.Request.Headers.AddHeader(headerName, "Bearer " + _account.AccessToken);
            }

            return;

            // Конвертируем Titanium.Web.Proxy запрос в HttpRequestMessage
            var httpRequestMessage = await ConvertToHttpRequestMessageAsync(e);

            try
            {
                // Отправляем запрос через HttpClient
                var responseMessage = await _httpClient.SendAsync(httpRequestMessage);

                // Чтение возможно сжатого контента
                var contentBytes = await responseMessage.Content.ReadAsByteArrayAsync();

                if (responseMessage.Content.Headers.ContentType?.MediaType?.StartsWith("text/") == true ||
                    responseMessage.Content.Headers.ContentType?.MediaType?.StartsWith("application/javascript") == true ||
                    responseMessage.Content.Headers.ContentType?.MediaType?.StartsWith("application/json") == true)
                {
                    // Проверяем, используется ли сжатие
                    if (responseMessage.Content.Headers.ContentEncoding.Contains("gzip"))
                    {
                        contentBytes = Decompressors.DecompressGzip(contentBytes);
                    }
                    else
                    if (responseMessage.Content.Headers.ContentEncoding.Contains("deflate"))
                    {
                        contentBytes = Decompressors.DecompressDeflate(contentBytes);
                    }
                    else
                    if (responseMessage.Content.Headers.ContentEncoding.Contains("br"))
                    {
                        contentBytes = Decompressors.DecompressBrotli(contentBytes);
                    }

                    // Определение кодировки
                    var encoding = GetEncodingFromHeaders(responseMessage) ?? Encoding.UTF8;

                    // Декодируем содержимое
                    var contentString = encoding.GetString(contentBytes);

                    // Убираем integrity
                    contentString = Regex.Replace(contentString, @"\s*integrity=[""'][^""']*[""']", "", RegexOptions.IgnoreCase);

                    // Изменение строки
                    contentString = contentString.Replace("s: \"Home\"", "s: \"MitM\"");

                    // Если нужно вернуть контент в байтах (например, для e.Ok()), конвертируем обратно
                    contentBytes = encoding.GetBytes(contentString);
                }

                // Получаем заголовки и передаем их вместе с контентом
                var headers = responseMessage.Headers
                    .Union(responseMessage.Content.Headers)
                    .Select(header => new HttpHeader(header.Key, string.Join(" ", header.Value)))
                    .ToList();

                // Добавляем CORS заголовки в ответ
                headers.RemoveAll(h => h.Name == "Access-Control-Allow-Origin");
                headers.RemoveAll(h => h.Name == "Access-Control-Allow-Methods");
                headers.Add(new HttpHeader("Access-Control-Allow-Origin", "*"));
                headers.Add(new HttpHeader("Access-Control-Allow-Methods", "GET,POST,PUT,DELETE,OPTIONS"));

                e.Ok(contentBytes, headers);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnRequestCaptureTraffic Exception: {ex.Message}");
            }
        }

        // Метод для определения кодировки из заголовков ответа
        private static Encoding? GetEncodingFromHeaders(HttpResponseMessage response)
        {
            if (response.Content.Headers.ContentType?.CharSet != null)
            {
                try
                {
                    return Encoding.GetEncoding(response.Content.Headers.ContentType.CharSet);
                }
                catch (ArgumentException)
                {
                    Debug.WriteLine($"Unsupported encoding: {response.Content.Headers.ContentType.CharSet}. Using default encoding.");
                }
            }
            return null;
        }

        private static async Task<HttpRequestMessage> ConvertToHttpRequestMessageAsync(SessionEventArgs e)
        {
            // Определяем HTTP-метод
            var method = new HttpMethod(e.HttpClient.Request.Method);

            // Создаем URI
            var uri = new Uri(e.HttpClient.Request.Url);

            // Создаем новый HttpRequestMessage
            var httpRequestMessage = new HttpRequestMessage(method, uri);

            // Добавляем заголовки
            foreach (var header in e.HttpClient.Request.Headers)
            {
                // Некоторые системные заголовки могут быть запрещены для установки вручную,
                // поэтому проверяем, можно ли их установить
                if (!httpRequestMessage.Headers.TryAddWithoutValidation(header.Name, header.Value))
                {
                    // Добавляем заголовки, которые не являются стандартными
                    httpRequestMessage.Content ??= new StringContent(string.Empty); // Пустой контент для заголовков
                    httpRequestMessage.Content.Headers.TryAddWithoutValidation(header.Name, header.Value);
                }
            }

            // Если есть тело запроса, добавляем его в HttpRequestMessage
            if (e.HttpClient.Request.HasBody)
            {
                var bodyBytes = await e.GetRequestBody();
                httpRequestMessage.Content = new ByteArrayContent(bodyBytes);

                // Копируем заголовки для контента
                foreach (var header in e.HttpClient.Request.Headers)
                {
                    httpRequestMessage.Content.Headers.TryAddWithoutValidation(header.Name, header.Value);
                }
            }

            return httpRequestMessage;
        }

        private static Task OnResponseCaptureTraffic(object sender, SessionEventArgs e)
        {
            Debug.WriteLine($"Captured response from: {e.HttpClient.Request.Url}");
            return Task.CompletedTask;
        }

        private static Task OnBeforeTunnelConnectRequest(object sender, TunnelConnectSessionEventArgs e)
        {
            string hostname = e.HttpClient.Request.RequestUri.Host;

            if (BLACK_LIST_HOSTS.Any(hostname.Contains))
            {
                e.DenyConnect = true;
            }

            string url = e.HttpClient.Request.RequestUriString;

            if (!WHITE_LIST_HOSTS.Any(hostname.Contains) || 
                url.Contains(".js") || url.Contains(".css") || url.Contains(".png") ||
                url.Contains(".jpg") || url.Contains(".jpeg") || url.Contains(".mp4") || url.Contains(".svg"))
            {
                e.DecryptSsl = false;
            }
            else
            {
                e.DecryptSsl = true;
            }

            return Task.CompletedTask;
        }

        // Allows overriding default certificate validation logic
        public static Task OnCertificateValidation(object sender, CertificateValidationEventArgs e)
        {
            // set IsValid to true/false based on Certificate Errors
            if (e.SslPolicyErrors == SslPolicyErrors.None) e.IsValid = true;

            return Task.CompletedTask;
        }

        // Allows overriding default client certificate selection logic during mutual authentication
        public Task OnCertificateSelection(object sender, CertificateSelectionEventArgs e)
        {
            e.ClientCertificate = GenerateCertificate(e.TargetHost);

            return Task.CompletedTask;
        }


        private static HttpClient GenerateClient(string? proxy = null, int connectTimeoutMs = 30000)
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
                        ICredentials credentials = new NetworkCredential(username, password);
                        proxyWeb = new WebProxy(proxyAddress, true, null, credentials);
                    }
                    else
                    {
                        proxyWeb = new WebProxy(proxyAddress);
                    }
                }
                else
                {
                    proxyAddress = proxy.Replace("socks5://", "").Replace("socks4://", "").Replace("http://", "");
                    proxyWeb = new WebProxy(proxyAddress);
                }

                handler.Proxy    = proxyWeb;
                handler.UseProxy = true;
            }

            HttpClient client = new(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(connectTimeoutMs)
            };
            return client;
        }

        private static bool ServerCertificateValidationCallback(HttpRequestMessage request, X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            // Допускаем только ошибку RemoteCertificateNameMismatch
            if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch)
            {
                Debug.WriteLine($"Ignoring certificate name mismatch for {request.RequestUri}");
                return true; // Игнорируем ошибку несоответствия имени
            }

            return false; // Не игнорируем другие ошибки сертификата
        }

        private X509Certificate2 GenerateCertificate(string hostname)
        {
            if (_certificates.TryGetValue(hostname, out var certificateFromDict))
            {
                return certificateFromDict;
            }

            var subjectName = $"CN={hostname}";
            var san = new SubjectAlternativeNameBuilder();
            san.AddDnsName(hostname);

            var certRequest = new CertificateRequest(
                new X500DistinguishedName(subjectName),
                RSA.Create(2048),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );

            // Добавляем Key Usage и EKU для сертификатов HTTPS
            certRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));
            var eku = new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }; // Server Authentication OID
            certRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(eku, true));

            certRequest.CertificateExtensions.Add(san.Build());

            var cert = certRequest.Create(
                _rootCert, // подписываем корневым сертификатом
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddYears(1),
                Guid.NewGuid().ToByteArray() // SerialNumber
            );

            var generatedCertificate = new X509Certificate2(cert.Export(X509ContentType.Pkcs12, "your_password_here"), "your_password_here");

            _certificates.TryAdd(hostname, generatedCertificate);

            return generatedCertificate;
        }


        private static X509Certificate2 GetRootCertificate()
        {
            string filename = Path.Combine(Environment.CurrentDirectory, "rootCert.pfx");
            if (!File.Exists(filename)) throw new FileNotFoundException(filename);

            return new X509Certificate2(filename);
        }
    }
}
