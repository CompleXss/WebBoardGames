using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using webapi.Data;
using webapi.Models;
using webapi.Repositories;
using webapi.Services;
using webapi.Swagger;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddDbContext<AppDbContext>(options =>
{
	options.UseSqlite(config.GetConnectionString("DefaultConnection"));
});
builder.Services.AddTransient<UsersRepository>();
builder.Services.AddTransient<UserRefreshTokenRepository>();
builder.Services.AddTransient<CheckersUserRepository>();
builder.Services.AddTransient<AuthService>();

// Configure Authentication & Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
	options.TokenValidationParameters = new TokenValidationParameters
	{
		ValidIssuer = config["Jwt:Issuer"],
		ValidAudience = config["Jwt:Audience"],
		IssuerSigningKey = new SymmetricSecurityKey
			(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
		ValidateIssuer = true,
		ValidateAudience = true,
		ValidateLifetime = true,
		ClockSkew = TimeSpan.Zero,
		ValidateIssuerSigningKey = true,
	};
});

builder.Services.AddAuthorization(options =>
{
	options.FallbackPolicy = new AuthorizationPolicyBuilder()
		.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
		.RequireAuthenticatedUser()
		.Build();
});

// Configure CORS
builder.Services.AddCors(options =>
{
	options.AddPolicy("CORSPolicy",
		builder =>
		{
			builder
			//.AllowAnyOrigin()
			.AllowAnyMethod()
			.AllowAnyHeader()
			.WithOrigins("http://localhost:3000");
		});
});

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


#region login & logout

// create user
// TODO: улетает в exception, если в body не было юзера
app.MapPost("/auth/register", async (UsersRepository users, AuthService auth, UserDto request) =>
{
	if (await users.GetAsync(request.Name) is not null)
		return Results.BadRequest($"User with this name ({request.Name}) already exists.");

	var user = auth.CreateUser(request);
	bool created = await users.AddAsync(user);

	if (!created)
		return Results.BadRequest($"Can not create user \"{request.Name}\".");

	var accessToken = auth.CreateAccessToken(user);
	return Results.Created($"/users/{user.Name}", new { user, accessToken });
}).AllowAnonymous();

app.MapPost("/auth/login", async (UsersRepository users, UserRefreshTokenRepository userTokens, AuthService auth, UserDto request) =>
{
	var user = await users.GetAsync(request.Name);

	if (user is null || !AuthService.VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
		return Results.NotFound("Username or password is invalid.");

	var accessToken = auth.CreateAccessToken(user);
	var refreshToken = await userTokens.AddRefreshTokenAsync(user.Id);

	if (refreshToken is null)
		return Results.BadRequest("Can not create refresh token.");

	return Results.Ok(new
	{
		accessToken,
		refreshToken,
	});
}).AllowAnonymous();

app.MapPost("/auth/refresh", async (HttpContext context, UserRefreshTokenRepository userTokens, AuthService auth) =>
{
	//var accessToken_str = await context.GetTokenAsync("access_token");
	var accessToken_str = context.Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
	var oldAccessToken = await auth.ValidateAccessToken_DontCheckExpireDate(accessToken_str);
	if (oldAccessToken is null)
		return Results.BadRequest("Invalid Access Token.");

	long userID = long.Parse(oldAccessToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)!.Value);
	string userName = oldAccessToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Name)!.Value;

	if (!context.Request.Cookies.TryGetValue("refresh_token", out var oldRefreshToken) || oldRefreshToken is null)
		return Results.BadRequest("Provided Refresh Token is null.");



	var userToken = await userTokens.GetAsync(userID, oldRefreshToken);
	if (userToken is null)
		return Results.BadRequest("Invalid Refresh Token.");

	if (userToken.RefreshToken != oldRefreshToken)
		return Results.BadRequest("Invalid Refresh Token.");

	if (DateTime.Parse(userToken.TokenExpires) < DateTime.UtcNow)
		return Results.BadRequest("Refresh Token expired.");

	var refreshToken = await userTokens.UpdateRefreshTokenAsync(userToken);
	if (refreshToken is null)
		return Results.BadRequest("Invalid Refresh Token.");

	var accessToken = auth.CreateAccessToken(userID, userName);

	return Results.Ok(new
	{
		accessToken,
		refreshToken,
	});
}).AllowAnonymous();

#endregion



#region Users

// get all users
app.MapGet("/users", async (UsersRepository users) =>
{
	return await users.GetAllAsync();
}).AllowAnonymous();
//.Produces

// get user by name
app.MapGet("/users/{username}", async (UsersRepository users, string username) =>
{
	var user = await users.GetAsync(username);

	return user != null
		? Results.Json(user)
		: Results.NotFound("User not found.");
});

// delete user by name
app.MapDelete("/users/{username}", async (UsersRepository users, string username) =>
{
	bool deleted = await users.DeleteAsync(username);

	return deleted
		? Results.Ok($"User \"{username}\" was deleted.")
		: Results.BadRequest($"Can not delete user \"{username}\".");
});

#endregion



app.Run();
