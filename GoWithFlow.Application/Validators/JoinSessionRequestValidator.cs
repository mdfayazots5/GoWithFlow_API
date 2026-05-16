using FluentValidation;
using GoWithFlow.Application.DTOs.Requests.Session;

namespace GoWithFlow.Application.Validators;

public sealed class JoinSessionRequestValidator : AbstractValidator<JoinSessionRequestDto>
{
	public JoinSessionRequestValidator()
	{
		RuleFor(request => request.JoinCode)
			.NotEmpty()
			.Length(6);

		RuleFor(request => request.SlotIndex)
			.InclusiveBetween((byte)1, (byte)5);
	}
}
