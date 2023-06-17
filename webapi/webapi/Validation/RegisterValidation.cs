using FluentValidation;
using webapi.Models;

namespace webapi.Validation;

public class RegisterValidation : AbstractValidator<UserDto>
{
	public RegisterValidation()
	{
		RuleFor(x => x.Name)
			.NotEmpty()
			.Length(3, 32);

		RuleFor(x => x.Password)
			.NotEmpty()
			.MinimumLength(3);
	}
}
