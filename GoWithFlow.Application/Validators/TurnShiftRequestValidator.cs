using FluentValidation;
using GoWithFlow.Application.DTOs.Requests.LiveSession;

namespace GoWithFlow.Application.Validators;

public sealed class TurnShiftRequestValidator : AbstractValidator<TurnShiftRequestDto>
{
	public TurnShiftRequestValidator()
	{
		RuleFor(request => request.SessionId)
			.GreaterThan(0);

		RuleFor(request => request.MemberId)
			.GreaterThan(0);

		RuleFor(request => request.TurnIndex)
			.GreaterThan(0);

		RuleFor(request => request.AnalysisScore)
			.InclusiveBetween(0, 100);
	}
}
