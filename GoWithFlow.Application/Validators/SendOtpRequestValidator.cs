using FluentValidation;
using GoWithFlow.Application.DTOs.Requests;

namespace GoWithFlow.Application.Validators;

public sealed class SendOtpRequestValidator : AbstractValidator<SendOtpRequestDto>
{
	public SendOtpRequestValidator()
	{
		RuleFor(request => request.MobileNumber)
			.NotEmpty()
			.Matches(@"^\d{10}$");
	}
}
