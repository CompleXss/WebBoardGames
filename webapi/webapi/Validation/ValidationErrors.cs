using FluentValidation.Results;

namespace webapi.Validation;

public static class ValidationErrors
{
	private static object Wrap(IEnumerable<string> errors)
	{
		return new { errors };
	}

	public static object From(string error)
	{
		return Wrap(new string[1] { error });
	}

	public static object From(IEnumerable<string> errors)
	{
		return Wrap(errors);
	}

	public static object From(IEnumerable<ValidationFailure> validationErrors)
	{
		var errors = validationErrors.Select(x => x.ErrorMessage);
		return Wrap(errors);
	}
}
