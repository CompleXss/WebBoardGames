using FluentValidation;
using webapi.Models;

namespace webapi.Validation;

public class RegisterValidation : AbstractValidator<UserRegisterDto>
{
	public RegisterValidation()
	{
		Include(new LoginValidation());

		RuleFor(x => x.Name)
			.NotEmpty()
			.MinimumLength(3);
	}
}
