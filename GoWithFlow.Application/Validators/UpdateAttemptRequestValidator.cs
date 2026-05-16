using FluentValidation;
using GoWithFlow.Application.DTOs.Requests.User;

namespace GoWithFlow.Application.Validators;

public sealed class UpdateAttemptRequestValidator : AbstractValidator<UpdateAttemptRequestDto>
{
	public UpdateAttemptRequestValidator()
	{
		RuleFor(request => request.RepracticeUtteranceId)
			.GreaterThan(0);

		RuleFor(request => request.Score)
			.InclusiveBetween(0, 100);
	}
}
