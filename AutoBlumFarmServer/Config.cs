using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AutoBlumFarmServer
{
    public class Config
    {
        private static Config  _instance = new();
        private IConfiguration _configuration;

        public Config()
        {
            // Билдер конфигураций
            var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.Api.json", optional: false, reloadOnChange: true);
            _configuration = builder.Build();

            string geoWithTZFilename = "geoWithTimezonesProxySeller.json";
            if (File.Exists(geoWithTZFilename))
            {
                string geoWithTZText = File.ReadAllText(geoWithTZFilename);
                var deserialized = JsonConvert.DeserializeObject<Dictionary<string, (string, int)>>(geoWithTZText);
                if (deserialized != null) GEO_PROXY_SELLER = deserialized;
            }
        }

        public static Config Instance 
        { 
            get { return _instance; }
            set { _instance = value; }
        }

        // COMMON PART
        public string  SERVER_DOMAIN                  => _configuration["ServerDomain"] ?? "";
        public string  PROXY_SELLER_API_KEY           => _configuration["ProxySellerAPIKey"] ?? "";
        public int     REFERRAL_BALANCE_BONUS_PERCENT => _configuration.GetValue("Referral:BalanceBonusPercent", 10);
        public decimal MAX_SLOT_PRICE                 => _configuration.GetValue("Prices:MaxSlotPrice",  0.99M);
        public decimal MIN_SLOT_PRICE                 => _configuration.GetValue("Prices:MinSlotPrice",  0.49M);
        public decimal STEP_SLOT_PRICE                => _configuration.GetValue("Prices:StepSlotPrice", 0.05M);
        public string  DEFAULT_AVATAR_PATH            => _configuration["DefaultAvatarPath"] ?? "defaultAvatar.png";

        // GEO PROXY SELLER
        public Dictionary<string, (string countryName, int timezoneOffset)> GEO_PROXY_SELLER = [];

        // JWT PART
        public string JWT_ISSUER            => _configuration["Jwt:Issuer"]   ?? "";
        public string JWT_AUDIENCE          => _configuration["Jwt:Audience"] ?? "";
        public string JWT_KEY               => _configuration["Jwt:Key"]      ?? "";
        public int    JWT_LIVE_HOURS        => _configuration.GetValue("Jwt:LiveHours", 8);

        // TELEGRAM PART
        public string TELEGRAM_CHANNEL_NAME               => _configuration["Telegram:ChannelName"] ?? "AutoBlumFarm";
        public long   TELEGRAM_TECH_SUPPORT_GROUP_CHAT_ID => _configuration.GetValue<long>("Telegram:TechSupportGroupChatId", 0);
        public string TELEGRAM_PUBLIC_BOT_NAME            => _configuration["Telegram:PublicBotName"] ?? "";
        public string TELEGRAM_BOT_TOKEN                  => _configuration["Telegram:BotToken"] ?? "";
        public string TELEGRAM_ADMIN_BOT_TOKEN            => _configuration["Telegram:AdminBotToken"] ?? "";
        public string TELEGRAM_WALLET_TOKEN               => _configuration["Telegram:WalletToken"] ?? "";

        // TELEGRAM STARS
        public string  TG_STARS_PAYMENT_INVOICE_PHOTO_URL    => _configuration["TelegramStarsPayment:InvoicePhotoUrl"] ?? "";
        public int     TG_STARS_PAYMENT_INVOICE_PHOTO_WIDTH  => _configuration.GetValue("TelegramStarsPayment:InvoicePhotoWidth", 700);
        public int     TG_STARS_PAYMENT_INVOICE_PHOTO_HEIGHT => _configuration.GetValue("TelegramStarsPayment:InvoicePhotoHeight", 370);
        public double  TG_STARS_PAYMENT_STAR_USD_PRICE       => _configuration.GetValue("TelegramStarsPayment:StarUsdPrice", 2.24);

        // SQL PART                         
        public string SQL_CONNECTION_STRING => _configuration["DatabaseSettings:ConnectionStrings:SqlConnectionString"] ?? "Data Source=blumbotfarm.db";

        // MONGO PART
        public string MONGO_CONNECTION_STRING => _configuration["DatabaseSettings:ConnectionStrings:MongoConnectionString"] ?? "";
        public string MONGO_DATABASE_NAME     => _configuration["DatabaseSettings:ConnectionStrings:MongoDatabaseName"] ?? "";

        // MONGO TABLES
        public string MONGO_ACCOUNT_PATH          => _configuration["DatabaseSettings:MongoTableNames:MONGO_ACCOUNT_PATH"] ?? "";
        public string MONGO_TASK_PATH             => _configuration["DatabaseSettings:MongoTableNames:MONGO_TASK_PATH"] ?? "";
        public string MONGO_MESSAGE_PATH          => _configuration["DatabaseSettings:MongoTableNames:MONGO_MESSAGE_PATH"] ?? "";
        public string MONGO_EARNING_PATH          => _configuration["DatabaseSettings:MongoTableNames:MONGO_EARNING_PATH"] ?? "";
        public string MONGO_REFERRAL_PATH         => _configuration["DatabaseSettings:MongoTableNames:MONGO_REFERRAL_PATH"] ?? "";
        public string MONGO_USER_PATH             => _configuration["DatabaseSettings:MongoTableNames:MONGO_USER_PATH"] ?? "";
        public string MONGO_WALLET_PAYMENT_PATH   => _configuration["DatabaseSettings:MongoTableNames:MONGO_WALLET_PAYMENT_PATH"] ?? "";
        public string MONGO_DAILY_REWARDS_PATH    => _configuration["DatabaseSettings:MongoTableNames:MONGO_DAILY_REWARDS_PATH"] ?? "";
        public string MONGO_STARS_PAYMENT_PATH    => _configuration["DatabaseSettings:MongoTableNames:MONGO_STARS_PAYMENT_PATH"] ?? "";
        public string MONGO_FEEDBACK_MESSAGE_PATH => _configuration["DatabaseSettings:MongoTableNames:MONGO_FEEDBACK_MESSAGE_PATH"] ?? "";

        private void ParseProxySellerGeo()
        {
            Dictionary<string, List<string>> contryCodeToTimezone = [];
            string timezonesText = File.ReadAllText("Resources\\timezones-per-country.json");
            try
            {
                var jsonObject = JObject.Parse(timezonesText.Replace("'", "\\'").Replace("\"", "'"));

                var iterator = jsonObject.First;
                while (iterator != null)
                {
                    string iteratorStr = iterator.ToString();
                    string countryKey  = iteratorStr[..(iteratorStr.IndexOf(':') - 1)].Replace("\"", "");

                    if (countryKey != "ALL")
                    {
                        contryCodeToTimezone.Add(countryKey, iterator.Values().Select(v => v.ToString().ToLower()).ToList());
                    }

                    iterator = iterator.Next;
                }
            }
            catch { }

            Dictionary<string, int> timezoneToOffset = [];
            string timezonesOffsetsText = File.ReadAllText("Resources\\timezones.json");
            try
            {
                dynamic jsonArray = JArray.Parse(timezonesOffsetsText.Replace("'", "\\'").Replace("\"", "'"));

                foreach (var obj in jsonArray)
                {
                    foreach (var tzname in obj.utc)
                    {
                        string realtzname = ((string)tzname).ToLower();
                        if (!timezoneToOffset.ContainsKey(realtzname))
                        {
                            timezoneToOffset.Add(realtzname, (int)obj.offset * 60 * (-1));
                        }
                    }
                }
            }
            catch { }

            const string geosFileName = "Resources\\geoProxySeller.json";
            if (File.Exists(geosFileName))
            {
                var geosJson = File.ReadAllText(geosFileName);
                try
                {
                    var jsonArray = JArray.Parse(geosJson.Replace("'", "\\'").Replace("\"", "'"));

                    var arr = jsonArray.ToArray();
                    for (int i = 0; i < arr.Length; i++)
                    {
                        var element = arr.ElementAt(i);

                        var countryCode = (string?)element["code"];
                        var countryName = (string?)element["name"];
                        if (countryCode != null && contryCodeToTimezone.ContainsKey(countryCode))
                        {
                            var timezonesForThisCountry = contryCodeToTimezone[countryCode];
                            foreach (var tz in timezonesForThisCountry)
                            {
                                if (timezoneToOffset.ContainsKey(tz) && !GEO_PROXY_SELLER.ContainsKey(countryCode)&&
                                    countryName != null)
                                {
                                    GEO_PROXY_SELLER.Add(countryCode, (countryName, timezoneToOffset[tz]));
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Not found country code: " + countryCode);
                        }
                    }
                }
                catch (Exception ex) { Console.WriteLine(ex); }
            }

            GEO_PROXY_SELLER.Add("BQ", ("Bonaire, Sint Eustatius, and Saba", 240));
            GEO_PROXY_SELLER.Add("CW", ("Curaçao", 240));
            GEO_PROXY_SELLER.Add("SX", ("Sint Maarten", 240));
            GEO_PROXY_SELLER.Add("XK", ("Kosovo", -120));

            File.WriteAllText("geoWithTimezonesProxySeller.json", JsonConvert.SerializeObject(GEO_PROXY_SELLER));
        }
    }
}
