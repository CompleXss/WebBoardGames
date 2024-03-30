using FluentValidation;
using webapi.Errors;

namespace webapi.Filters;

public class ValidationFilter<T> : IEndpointFilter where T : class
{
	private readonly IValidator<T> validator;

	public ValidationFilter(IValidator<T> validator)
	{
		this.validator = validator;
	}

	public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
	{
		var obj = context.Arguments.SingleOrDefault(x => x?.GetType() == typeof(T));
		if (obj is not T validatable)
			return Results.BadRequest($"Body is missing object of type {typeof(T)}.");

		var validationResult = await validator.ValidateAsync(validatable);

		if (!validationResult.IsValid)
			return ValidationErrors.FirstError(validationResult.Errors);

		return await next(context);
	}
}
