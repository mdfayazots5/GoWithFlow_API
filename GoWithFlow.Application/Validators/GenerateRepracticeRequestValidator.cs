using FluentValidation;
using GoWithFlow.Application.DTOs.Requests.User;

namespace GoWithFlow.Application.Validators;

public sealed class GenerateRepracticeRequestValidator : AbstractValidator<GenerateRepracticeRequestDto>
{
	public GenerateRepracticeRequestValidator()
	{
		// A specific source session is required only for single-session repractice.
		// For "Practice All Mistakes" (IncludeAllSessions = true) the source session is
		// optional — the service derives a valid FK anchor from the loaded mistakes.
		RuleFor(request => request.SourceSessionId)
			.GreaterThan(0)
			.When(request => !request.IncludeAllSessions);
	}
}
