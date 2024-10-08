﻿using FluentValidation;
using webapi.Models;

namespace webapi.Validation;

public class ChangePasswordValidation : AbstractValidator<ChangeUserPasswordDto>
{
	public ChangePasswordValidation()
	{
		RuleFor(x => x.OldPassword)
			.NotEmpty()
			.MinimumLength(8);

		RuleFor(x => x.NewPassword)
			.NotEmpty()
			.MinimumLength(8);
	}
}
