using FluentValidation;
using GoWithFlow.Application.DTOs.Requests;

namespace GoWithFlow.Application.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
	public LoginRequestValidator()
	{
		RuleFor(x => x.MobileNumber)
			.NotEmpty()
			.Matches(@"^\d{10}$");

		RuleFor(x => x.Password)
			.NotEmpty()
			.MinimumLength(6);
	}
}
