using System.Runtime.CompilerServices;

namespace webapi.Errors;

public static class UserErrors
{
	private const string ERROR_CODE = "User.";

	/// <summary> <inheritdoc cref="Errors.BadRequest"/> </summary>
	public static IResult CouldNotCreate(string login)
	{
		return GetResult(Errors.BadRequest, $"Could not create user `{login}`");
	}



	private static IResult GetResult(Func<string, string, IEnumerable<object>?, IResult> func, string message, IEnumerable<object>? errors = null, [CallerMemberName] string? callerName = null)
	{
		callerName ??= "Unexpected";
		return func(ERROR_CODE + callerName, message, errors);
	}
}
