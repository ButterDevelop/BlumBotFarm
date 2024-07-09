using AutoBlumFarmServer;
using AutoBlumFarmServer.ApiResponses;
using AutoBlumFarmServer.ApiResponses.AccountController;
using AutoBlumFarmServer.ApiResponses.TelegramAuthController;
using BlumBotFarm.Database;
using BlumBotFarm.Database.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Text;

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

// I don't will joy for this, but I have no time actually to do some patterns like Strategy
Database.Initialize();
var databaseConnection = Database.GetConnection();
builder.Services.AddScoped(provider => new AccountRepository(databaseConnection));
builder.Services.AddScoped(provider => new UserRepository(databaseConnection));

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
