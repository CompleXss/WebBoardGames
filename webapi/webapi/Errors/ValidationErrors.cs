using FluentValidation.Results;
using System.Runtime.CompilerServices;

namespace webapi.Errors;

public static class ValidationErrors
{
	private const string ERROR_CODE = "Validation.";

	/// <summary> <inheritdoc cref="Errors.BadRequest"/> </summary>
	public static IResult FirstError(List<ValidationFailure> errors)
	{
		string? errorCode = null;
		string errorMessage = "Validation error";

		if (errors.Count > 0)
		{
			errorCode = errors[0].ErrorCode;
			errorMessage = errors[0].ErrorMessage;
		}

		return GetResult(Errors.BadRequest, errorMessage, callerName: errorCode);
	}

	/// <summary> <inheritdoc cref="Errors.BadRequest"/> </summary>
	public static IResult AllErrors(List<ValidationFailure> errors)
	{
		string? errorCode = errors.Count > 0 ? errors[0].ErrorCode : null;
		var errorsMessages = errors.Select(x => x.ErrorMessage);

		return GetResult(Errors.BadRequest, "Validation error", errorsMessages, callerName: errorCode);
	}



	private static IResult GetResult(Func<string, string, IEnumerable<object>?, IResult> func, string message, IEnumerable<object>? errors = null, [CallerMemberName] string? callerName = null)
	{
		callerName ??= "Unexpected";
		return func(ERROR_CODE + callerName, message, errors);
	}
}
