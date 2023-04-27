using webapi.Models;
using webapi.Repositories;

namespace webapi.Endpoints;

public static class UserEndpoints
{
	public static void MapUserEndpoints(this WebApplication app)
	{
		app.MapGet("/users", GetAllAsync)
			.AllowAnonymous()
			.Produces<List<User>>();

		app.MapGet("/users/{username}", GetAsync);
		app.MapDelete("/users/{username}", DeleteAsync);
	}

	internal static async Task<IResult> GetAllAsync(UsersRepository users)
	{
		return Results.Ok(await users.GetAllAsync());
	}

	internal static async Task<IResult> GetAsync(UsersRepository users, string username)
	{
		var user = await users.GetAsync(username);

		return user != null
			? Results.Json(user)
			: Results.NotFound("User not found.");
	}

	internal static async Task<IResult> DeleteAsync(UsersRepository users, string username)
	{
		bool deleted = await users.DeleteAsync(username);

		return deleted
			? Results.Ok($"User \"{username}\" was deleted.")
			: Results.BadRequest($"Can not delete user \"{username}\".");
	}
}
