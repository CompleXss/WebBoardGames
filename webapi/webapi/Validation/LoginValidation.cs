using FluentValidation;
using webapi.Models;

namespace webapi.Validation;

public class LoginValidation : AbstractValidator<UserLoginDto>
{
	public LoginValidation()
	{
		RuleFor(x => x.Login)
			.NotEmpty()
			.Length(3, 32)
			.Must(x => x.All(c => !char.IsWhiteSpace(c))); // should not have white spaces

		// TODO: password validation
		RuleFor(x => x.Password)
			.NotEmpty()
			.MinimumLength(3);
	}
}
