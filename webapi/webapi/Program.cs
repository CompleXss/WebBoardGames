using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
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
	options.UseSqlite(config.GetConnectionString("DefaultConnection"));
});
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

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();



app.MapAuthEndpoints();
app.MapUsersEndpoints();
app.MapPlayHistoryEndpoints();
app.MapLeaderboardEndpoints();

// games
app.MapGameEndpoints();



app.Run();



// TODO: добавить фильтры