using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using webapi.Data;
using webapi.Endpoints;
using webapi.Repositories;
using webapi.Services;
using webapi.Configuration;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddSignalR();

builder.Services.AddDbContext<AppDbContext>(options =>
{
	options.UseSqlite(config.GetConnectionString("DefaultConnection"));
});

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Configure services
builder.Services.AddTransient<AuthService>();
builder.Services.AddTransient<UserRefreshTokenRepository>();
builder.Services.AddTransient<UsersRepository>();
builder.Services.AddTransient<CheckersUserRepository>();
builder.Services.AddTransient<CheckersHistoryRepository>();
builder.Services.AddTransient<GameHistoryService>();

builder.Services.AddSingleton<CheckersLobbyService>();
builder.Services.AddSingleton<CheckersGameService>();

// Configure Authentication & Authorization
builder.Services.ConfigureAuthentication(config);
builder.Services.ConfigureAuthorization();

// Configure CORS
builder.Services.AddCORSPolicy();

// Add swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();



var app = builder.Build();

app.UseCORSPolicy();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}
else
	app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();



// global exception handling
app.Map("/error", (HttpContext context) =>
{
	var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
	var exception = exceptionHandlerPathFeature?.Error;

	return Results.Problem(title: exception?.Message);
});

app.UseExceptionHandler("/error");



app.MapAuthEndpoints();
app.MapUsersEndpoints();
app.MapPlayHistoryEndpoints();
app.MapLeaderboardEndpoints();

app.MapGameEndpoints();



app.Run();
