using AutoBlumFarmServer.ApiResponses;
using AutoBlumFarmServer.ApiResponses.AccountController;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;

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

app.UseAuthorization();

app.MapControllers();

app.Run();
