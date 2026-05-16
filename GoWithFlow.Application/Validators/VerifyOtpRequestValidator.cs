using FluentValidation;
using GoWithFlow.Application.DTOs.Requests;

namespace GoWithFlow.Application.Validators;

public sealed class VerifyOtpRequestValidator : AbstractValidator<VerifyOtpRequestDto>
{
	public VerifyOtpRequestValidator()
	{
		RuleFor(request => request.MobileNumber)
			.NotEmpty()
			.Matches(@"^\d{10}$");

		RuleFor(request => request.OtpCode)
			.NotEmpty()
			.Matches(@"^\d{6}$");
	}
}
