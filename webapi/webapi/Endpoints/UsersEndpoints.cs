using Microsoft.AspNetCore.Authentication;
using webapi.Data;
using webapi.Models;
using webapi.Repositories;
using webapi.Services;

namespace webapi.Endpoints;

public static class UsersEndpoints
{
	public static void MapUsersEndpoints(this WebApplication app)
	{
		app.MapGet("/users", GetAllAsync)
			.AllowAnonymous()
			.Produces<List<User>>();

		app.MapGet("/user", GetAsync)
			.Produces<User>();

		app.MapDelete("/user", DeleteAsync);
	}



	internal static async Task<IResult> GetAllAsync(UsersRepository users)
	{
		return Results.Ok(await users.GetAllAsync());
	}

	internal static async Task<IResult> GetAsync(HttpContext context, AuthService auth, UsersRepository users)
	{
		var accessToken = await context.GetTokenAsync(AuthEndpoint.ACCESS_TOKEN_COOKIE_NAME);
		if (accessToken is null) return Results.Unauthorized();

		(long userID, string username) = auth.GetUserInfoFromAccessToken(accessToken);
		var user = await users.GetAsync(userID);

		return user != null
			? Results.Ok(user)
			: Results.NotFound($"User '{username}' not found.");
	}

	internal static async Task<IResult> DeleteAsync(HttpContext context, AuthService auth, UsersRepository users, AppDbContext db)
	{
		var accessToken = await context.GetTokenAsync(AuthEndpoint.ACCESS_TOKEN_COOKIE_NAME);
		if (accessToken is null) return Results.Unauthorized();

		(long userID, string username) = auth.GetUserInfoFromAccessToken(accessToken);

		bool deleted = await users.DeleteAsync(userID);
		if (deleted)
		{
			AuthEndpoint.DeleteTokenCookies(context.Response);
			return Results.Ok($"User '{username}' was deleted.");
		}
		else
			return Results.BadRequest($"Can not delete user '{username}'.");
	}
}
