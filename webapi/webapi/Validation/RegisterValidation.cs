using FluentValidation;
using webapi.Models;

namespace webapi.Validation;

public class RegisterValidation : AbstractValidator<UserRegisterDto>
{
	public RegisterValidation()
	{
		ClassLevelCascadeMode = CascadeMode.Stop;

		Include(new LoginValidation());

		RuleFor(x => x.Login)
			.Must(x => x.All(c => !char.IsWhiteSpace(c)))
			.WithErrorCode("WhiteSpaceValidator")
			.WithMessage(x => $"'{nameof(x.Login)}' should not have white spaces.");

		RuleFor(x => x.Name)
			.NotEmpty()
			.MinimumLength(1);
	}
}
