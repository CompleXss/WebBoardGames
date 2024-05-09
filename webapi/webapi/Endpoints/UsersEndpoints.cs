using webapi.Models.Data;
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

		app.MapGet("/user/{userPublicID}", GetByPublicIDAsync)
			.AllowAnonymous()
			.Produces<User>();

		app.MapDelete("/user", DeleteAsync);
	}



	internal static async Task<IResult> GetAllAsync(UsersRepository usersRepository)
	{
		return Results.Ok(await usersRepository.GetAllAsync());
	}

	internal static async Task<IResult> GetAsync(HttpContext context, UsersRepository usersRepository)
	{
		var userInfo = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (userInfo is null) return Results.Unauthorized();

		var user = await usersRepository.GetByPublicIdAsync(userInfo.PublicID);

		return user != null
			? Results.Ok(user)
			: Results.NotFound("User not found");
	}

	internal static async Task<IResult> GetByPublicIDAsync(UsersRepository usersRepository, string userPublicID)
	{
		var user = await usersRepository.GetByPublicIdAsync(userPublicID);

		return user != null
			? Results.Ok(user)
			: Results.NotFound("User not found");
	}

	internal static async Task<IResult> DeleteAsync(HttpContext context, UsersRepository usersRepository, AuthService auth)
	{
		var userInfo = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (userInfo is null) return Results.Unauthorized();

		var user = await usersRepository.GetByPublicIdAsync(userInfo.PublicID);
		if (user is null)
			return Results.BadRequest("Could not delete. User not found");

		bool deleted = await usersRepository.DeleteAsync(user);
		if (deleted)
		{
			auth.DeleteTokenCookies(context.Response);
			return Results.Ok($"User `{user.Name}` was deleted");
		}

		return Results.BadRequest($"Could not delete user `{user.Name}`");
	}
}
