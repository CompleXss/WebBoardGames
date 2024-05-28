using webapi.Filters;
using webapi.Models;
using webapi.Models.Data;
using webapi.Repositories;
using webapi.Services;
using webapi.Validation;

namespace webapi.Endpoints;

public static class UsersEndpoints
{
	public static void MapUsersEndpoints(this WebApplication app)
	{
		app.MapGet("/user", GetAsync)
			.Produces<User>();

		app.MapGet("/user/{userPublicID}", GetByPublicIDAsync)
			.AllowAnonymous()
			.Produces<UserPublicInfo>();

		app.MapPost("/user/edit/name", EditNameAsync);
		app.MapPost("/user/edit/login", EditLoginAsync);
		app.MapPost("/user/edit/password", EditPasswordAsync)
			.AddEndpointFilter<ValidationFilter<ChangeUserPasswordDto>>();

		app.MapPost("/user", DeleteAsync);
	}



	internal static async Task<IResult> GetAsync(HttpContext context, UsersRepository usersRepository)
	{
		var userInfo = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (userInfo is null) return Results.Unauthorized();

		var user = await usersRepository.GetByPublicIdAsync(userInfo.PublicID);

		return user is not null
			? Results.Ok(user)
			: Results.NotFound("User not found");
	}

	internal static async Task<IResult> GetByPublicIDAsync(UsersRepository usersRepository, string userPublicID)
	{
		var user = await usersRepository.GetByPublicIdAsync(userPublicID);
		if (user is null)
			return Results.NotFound("User not found");

		var dto = new UserPublicInfo
		{
			PublicID = user.PublicID,
			Name = user.Name,
		};

		return Results.Ok(dto);
	}

	internal static async Task<IResult> EditNameAsync(HttpContext context, UsersRepository usersRepository, string name)
	{
		var userInfo = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (userInfo is null) return Results.Unauthorized();

		if (string.IsNullOrWhiteSpace(name) || name.Length < 1)
			return Results.BadRequest("Provided Name is not valid");

		bool success = await usersRepository.UpdateUserName(userInfo.PublicID, name);
		return success
			? Results.Ok()
			: Results.BadRequest("Could not update user name");
	}

	internal static async Task<IResult> EditLoginAsync(HttpContext context, UsersRepository usersRepository, string login)
	{
		var userInfo = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (userInfo is null) return Results.Unauthorized();

		if (string.IsNullOrWhiteSpace(login) || login.Length < 3 || login.Length > 32 || login.Any(c => char.IsWhiteSpace(c)))
			return Results.BadRequest("Provided Login is not valid");

		bool success = await usersRepository.UpdateUserLogin(userInfo.PublicID, login);
		return success
			? Results.Ok()
			: Results.BadRequest("Could not update user login");
	}

	internal static async Task<IResult> EditPasswordAsync(HttpContext context, AuthService auth, UsersRepository usersRepository, ChangeUserPasswordDto dto)
	{
		var userInfo = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (userInfo is null) return Results.Unauthorized();

		var user = await usersRepository.GetByPublicIdAsync(userInfo.PublicID);
		if (user is null)
			return Results.NotFound("User not found");

		if (!auth.VerifyPasswordHash(dto.OldPassword, user.PasswordHash, user.PasswordSalt))
			return Results.BadRequest("Provided old password is not valid");

		auth.CreatePasswordHash(dto.NewPassword, out var hash, out var salt);

		bool success = await usersRepository.UpdateUserPassword(userInfo.PublicID, hash, salt);
		return success
			? Results.Ok()
			: Results.BadRequest("Could not update user password");
	}



	internal static async Task<IResult> DeleteAsync(HttpContext context, UsersRepository usersRepository, AuthService auth, UserPasswordDto dto)
	{
		var userInfo = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (userInfo is null) return Results.Unauthorized();

		var user = await usersRepository.GetByPublicIdAsync(userInfo.PublicID);
		if (user is null)
			return Results.BadRequest("Could not delete. User not found");

		if (!auth.VerifyPasswordHash(dto.Password, user.PasswordHash, user.PasswordSalt))
			return Results.BadRequest("Could not delete. Incorrect password");

		bool deleted = await usersRepository.DeleteAsync(user);
		if (deleted)
		{
			auth.DeleteTokenCookies(context.Response);
			return Results.Ok($"User `{user.Name}` was deleted");
		}

		return Results.BadRequest($"Could not delete user `{user.Name}`");
	}
}
