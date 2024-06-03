using FluentValidation;
using webapi.Models;

namespace webapi.Validation;

public class LoginValidation : AbstractValidator<UserLoginDto>
{
	public LoginValidation()
	{
		ClassLevelCascadeMode = CascadeMode.Stop;

		RuleFor(x => x.Login)
			.NotEmpty()
			.Length(3, 32);

		RuleFor(x => x.Password)
			.NotEmpty()
			.MinimumLength(8);
	}
}
