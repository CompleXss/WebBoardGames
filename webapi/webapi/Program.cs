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

builder.Services.AddDbContext<AppDbContext>(options =>
{
	options.UseSqlite(config.GetConnectionString("DefaultConnection"));
});
builder.Services.AddTransient<UsersRepository>();
builder.Services.AddTransient<UserRefreshTokenRepository>();
builder.Services.AddTransient<CheckersUserRepository>();
builder.Services.AddTransient<CheckersHistoryRepository>();
builder.Services.AddTransient<AuthService>();

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

app.UseCors("CORSPolicy");

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



app.Run();



// TODO: добавить фильтры