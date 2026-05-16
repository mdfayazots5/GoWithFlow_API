using FluentValidation;
using GoWithFlow.Application.DTOs.Requests.Session;

namespace GoWithFlow.Application.Validators;

public sealed class UpdateReadyStatusRequestValidator : AbstractValidator<UpdateReadyStatusRequestDto>
{
	public UpdateReadyStatusRequestValidator()
	{
		RuleFor(request => request.SessionId)
			.GreaterThan(0);
	}
}
