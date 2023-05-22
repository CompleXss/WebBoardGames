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

		app.MapGet("/user/{userID}", GetByIDAsync)
			.Produces<User>();

		app.MapDelete("/user", DeleteAsync);
	}



	internal static async Task<IResult> GetAllAsync(UsersRepository users)
	{
		return Results.Ok(await users.GetAllAsync());
	}

	internal static async Task<IResult> GetAsync(HttpContext context, UsersRepository users)
	{
		var userInfo = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (userInfo is null) return Results.Unauthorized();

		var user = await users.GetAsync(userInfo.ID);

		return user != null
			? Results.Ok(user)
			: Results.NotFound($"User '{userInfo.Name}' not found.");
	}

	internal static async Task<IResult> GetByIDAsync(UsersRepository users, long userID)
	{
		var user = await users.GetAsync(userID);

		return user != null
			? Results.Ok(user)
			: Results.NotFound($"User with ID '{userID}' not found.");
	}

	internal static async Task<IResult> DeleteAsync(HttpContext context, UsersRepository users, AppDbContext db)
	{
		var user = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (user is null) return Results.Unauthorized();

		bool deleted = await users.DeleteAsync(user.ID);
		if (deleted)
		{
			AuthEndpoint.DeleteTokenCookies(context.Response);
			return Results.Ok($"User '{user.Name}' was deleted.");
		}
		else
			return Results.BadRequest($"Can not delete user '{user.Name}'.");
	}
}
