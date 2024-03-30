using System.Runtime.CompilerServices;

namespace webapi.Errors;

public static class AuthErrors
{
	private const string ERROR_CODE = "Auth.";

	/// <summary> <inheritdoc cref="Errors.BadRequest"/> </summary>
	public static IResult UserAlreadyExists(string login)
	{
		return GetResult(Errors.BadRequest, $"User with login `{login}` already exists");
	}

	/// <summary> <inheritdoc cref="Errors.ServerError"/> </summary>
	public static IResult CouldNotCreateTokenPair()
	{
		return GetResult(Errors.ServerError, "Could not add new token pair to cookies");
	}



	private static IResult GetResult(Func<string, string, IEnumerable<object>?, IResult> func, string message, IEnumerable<object>? errors = null, [CallerMemberName] string? callerName = null)
	{
		callerName ??= "Unexpected";
		return func(ERROR_CODE + callerName, message, errors);
	}
}
