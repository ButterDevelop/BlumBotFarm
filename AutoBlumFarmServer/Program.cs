using AutoBlumFarmServer;
using AutoBlumFarmServer.ApiResponses;
using AutoBlumFarmServer.ApiResponses.AccountController;
using AutoBlumFarmServer.ApiResponses.TelegramAuthController;
using AutoBlumFarmServer.ApiResponses.UserController;
using BlumBotFarm.Core;
using BlumBotFarm.Database;
using BlumBotFarm.Database.Repositories;
using BlumBotFarm.MessageProcessor;
using BlumBotFarm.TelegramBot;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Swashbuckle.AspNetCore.Filters;
using System.Text;
using Telegram.Bot;

Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
Thread.CurrentThread.CurrentCulture   = System.Globalization.CultureInfo.GetCultureInfo("en-US");

string MASK_DATE_LOG_FILE_PATH = "%DateTime%", LOG_FILE_PATH = $"logs/blumBotFarm-{MASK_DATE_LOG_FILE_PATH}.log";
Log.Logger = new LoggerConfiguration()
                         .MinimumLevel.Debug()
                         .WriteTo.Console()
                         .WriteTo.File(LOG_FILE_PATH.Replace(MASK_DATE_LOG_FILE_PATH, ""), rollingInterval: RollingInterval.Hour)
                         .CreateLogger();

// Настройка Telegram-бота через конфигурацию
var botToken       = Config.Instance.TELEGRAM_BOT_TOKEN;
var adminUsernames = AppConfig.BotSettings.AdminUsernames;
var adminChatIds   = AppConfig.BotSettings.AdminChatIds;
if (botToken != null && adminUsernames != null && adminChatIds != null)
{
    var adminTelegramBot = new TelegramBot(botToken, adminUsernames, adminChatIds,
                                           Config.Instance.TG_STARS_PAYMENT_STAR_USD_PRICE,
                                           Config.Instance.REFERRAL_BALANCE_BONUS_PERCENT);
    adminTelegramBot.Start();

    Log.Information("Started Telegram bot.");
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

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

// I don't will joy for this, but I have no time actually to do some patterns like Strategy
Database.Initialize();
var databaseConnection = Database.GetConnection();
builder.Services.AddScoped(provider => new AccountRepository(databaseConnection));
builder.Services.AddScoped(provider => new UserRepository(databaseConnection));
builder.Services.AddScoped(provider => new ReferralRepository(databaseConnection));
builder.Services.AddScoped(provider => new WalletPaymentRepository(databaseConnection));
builder.Services.AddScoped(provider => new TelegramBotClient(Config.Instance.TELEGRAM_BOT_TOKEN));

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();