using AutoBlumFarmServer;
using AutoBlumFarmServer.Helpers;
using AutoBlumFarmServer.SwaggerApiResponses;
using AutoBlumFarmServer.SwaggerApiResponses.AccountController;
using AutoBlumFarmServer.SwaggerApiResponses.PurchaseController;
using AutoBlumFarmServer.SwaggerApiResponses.TelegramAuthController;
using AutoBlumFarmServer.SwaggerApiResponses.UserController;
using BlumBotFarm.CacheUpdater;
using BlumBotFarm.CacheUpdater.CacheServices;
using BlumBotFarm.Core;
using BlumBotFarm.Database;
using BlumBotFarm.Database.Repositories;
using BlumBotFarm.TelegramBot;
using BlumBotFarm.Translation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Swashbuckle.AspNetCore.Filters;
using System.Text;
using Telegram.Bot;

Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
Thread.CurrentThread.CurrentCulture   = System.Globalization.CultureInfo.GetCultureInfo("en-US");

// Инициализация UserAgents
HTTPController.Initialize(AutoBlumFarmServer.Properties.Resources.AndroidBoughtUserAgents);

string MASK_DATE_LOG_FILE_PATH = "%DateTime%", LOG_FILE_PATH = $"logs/blumBotFarm-{MASK_DATE_LOG_FILE_PATH}.log";
Log.Logger = new LoggerConfiguration()
                         .MinimumLevel.Debug()
                         .WriteTo.Console()
                         .WriteTo.File(LOG_FILE_PATH.Replace(MASK_DATE_LOG_FILE_PATH, ""), rollingInterval: RollingInterval.Hour)
                         .CreateLogger();

// Настройка Telegram-бота через конфигурацию
var botToken       = Config.Instance.TELEGRAM_BOT_TOKEN;
var adminChatIds   = AppConfig.BotSettings.AdminChatIds;
if (botToken != null && adminChatIds != null)
{
    var adminTelegramBot = new TelegramBot(botToken, adminChatIds,
                                           Config.Instance.TG_STARS_PAYMENT_STAR_USD_PRICE,
                                           Config.Instance.REFERRAL_BALANCE_BONUS_PERCENT,
                                           Config.Instance.SERVER_DOMAIN,
                                           Config.Instance.TELEGRAM_PUBLIC_BOT_NAME,
                                           Config.Instance.TELEGRAM_TECH_SUPPORT_GROUP_CHAT_ID,
                                           Config.Instance.TELEGRAM_CHANNEL_NAME);
    adminTelegramBot.Start();

    Log.Information("Started Telegram bot.");
}

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("https://localhost:5000");

// Add services to the container.

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
// Add Swagger and configure it
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My backend Web API v1", Version = "v1" });
    c.ExampleFilters();  // Enable examples
    c.EnableAnnotations();
});

// Register examples from the assembly
builder.Services.AddSwaggerExamplesFromAssemblyOf<BadAuthExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<GetAllAccountsOkExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<GetAccountByIdOkExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<GetAccountById400BadExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<CheckAccountUsernameOkExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<CheckAccountUsernameBadExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<UpdateAccountOkExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<UpdateAccountBadExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<TelegramAuthOkExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<TelegramAuthBadExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<MyReferralsOkExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<StarsPaymentTransactionOkExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<StarsPaymentTransactionBadExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<ChangeUsersReferralCodeOkExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<ChangeUsersReferralCodeBadExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<AboutMeUserInfoOkExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<MyReferralsOkExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<StarsPaymentTransactionOkExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<StarsPaymentTransactionBadExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<ConvertStarsToUsdOkExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<ConvertUsdToStarsOkExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<ConvertCurrenciesBadExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<MyPaymentTransactionsOkExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<BuyAccountsSlotsOkExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<BuyAccountsSlotsBadExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<PreBuyAccountsSlotsOkExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<PreBuyAccountsSlotsBadExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<AllGeoOkExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<GetTranslationsOkExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<GetTranslationsBadExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<GetAvailableLanguagesOkExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<ChangeLanguageOkExample>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<ChangeLanguageBadExample>();

builder.Services.AddMemoryCache();

// I don't will joy for this, but I have no time actually to do some patterns like Strategy
var databaseConnection = Config.Instance.MONGO_CONNECTION_STRING;
var databaseName       = Config.Instance.MONGO_DATABASE_NAME;
builder.Services.AddScoped(provider => new AccountRepository(databaseConnection, databaseName, Config.Instance.MONGO_ACCOUNT_PATH));
builder.Services.AddScoped(provider => new UserRepository(databaseConnection, databaseName, Config.Instance.MONGO_USER_PATH));
builder.Services.AddScoped(provider => new ReferralRepository(databaseConnection, databaseName, Config.Instance.MONGO_REFERRAL_PATH));
builder.Services.AddScoped(provider => new StarsPaymentRepository(databaseConnection, databaseName, Config.Instance.MONGO_STARS_PAYMENT_PATH));
builder.Services.AddScoped(provider => new DailyRewardRepository(databaseConnection, databaseName, Config.Instance.MONGO_DAILY_REWARDS_PATH));
builder.Services.AddScoped(provider => new EarningRepository(databaseConnection, databaseName, Config.Instance.MONGO_EARNING_PATH));
builder.Services.AddScoped(provider => new TaskRepository(databaseConnection, databaseName, Config.Instance.MONGO_TASK_PATH));
builder.Services.AddScoped(provider => new TelegramBotClient(Config.Instance.TELEGRAM_BOT_TOKEN));
builder.Services.AddScoped(provider => new ProxySellerAPIHelper(Config.Instance.PROXY_SELLER_API_KEY));
builder.Services.AddScoped(provider => new TranslationHelper());
builder.Services.AddScoped<IUserCacheService, UserCacheService>();

// Настройка аутентификации JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = Config.Instance.JWT_ISSUER,
            ValidAudience            = Config.Instance.JWT_AUDIENCE,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Config.Instance.JWT_KEY))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var userCacheService = scope.ServiceProvider.GetRequiredService<IUserCacheService>();
    if (userCacheService != null)
    {
        // Starting Cache Updater
        var cacheUpdater = new CacheUpdater(new CancellationToken(), userCacheService);
        await Task.Factory.StartNew(cacheUpdater.StartAsync);
        Log.Information("Started Auto Cache Updater.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My backend Web API v1");
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAllOrigins"); // Применение политики CORS

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();