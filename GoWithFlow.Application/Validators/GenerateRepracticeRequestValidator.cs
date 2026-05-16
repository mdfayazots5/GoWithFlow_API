using FluentValidation;
using GoWithFlow.Application.DTOs.Requests.User;

namespace GoWithFlow.Application.Validators;

public sealed class GenerateRepracticeRequestValidator : AbstractValidator<GenerateRepracticeRequestDto>
{
	public GenerateRepracticeRequestValidator()
	{
		RuleFor(request => request.SourceSessionId)
			.GreaterThan(0);
	}
}
