using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using FluentValidation;
using webapi.Data;
using webapi.Endpoints;
using webapi.Repositories;
using webapi.Services;
using webapi.Configuration;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddSignalR();

builder.Services.AddDbContext<AppDbContext>(options =>
{
	options.UseNpgsql(config.GetConnectionString("DefaultConnection"));
});

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Configure services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<GameHistoryService>();
builder.Services.AddGameServices();

// Configure repositories
builder.Services.AddScoped<UsersRepository>();
builder.Services.AddScoped<UserRefreshTokenRepository>();
builder.Services.AddScoped<GamesRepository>();
builder.Services.AddScoped<GameHistoryRepository>();
builder.Services.AddScoped<UserGameStatisticsRepository>();

// Configure Authentication & Authorization
builder.Services.ConfigureAuthentication(config);
builder.Services.ConfigureAuthorization();

// Configure rate limiting
builder.Services.ConfigureRateLimiting();

// Configure CORS
builder.Services.AddCORSPolicy(config);

// Add swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();



var app = builder.Build();
if (!await app.Services.CheckDatabaseIsReadyAsync(app.Logger))
{
	throw new Exception("Database problem. See logs for details.");
}

app.UseCORSPolicy();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();

	// Redirect to swagger page
	app.Map("/", (HttpResponse response) =>
	{
		response.Redirect("/swagger");
	}).AllowAnonymous();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();



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
