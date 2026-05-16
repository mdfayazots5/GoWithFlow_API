using FluentValidation;
using GoWithFlow.Application.DTOs.Requests.LiveSession;

namespace GoWithFlow.Application.Validators;

public sealed class ListenerFeedbackRequestValidator : AbstractValidator<ListenerFeedbackRequestDto>
{
	private static readonly string[] ValidTags =
	{
		"Good",
		"Hesitated",
		"Mistake",
		"Unclear Pronunciation"
	};

	public ListenerFeedbackRequestValidator()
	{
		RuleFor(request => request.SessionId)
			.GreaterThan(0);

		RuleFor(request => request.TurnIndex)
			.GreaterThan(0);

		RuleFor(request => request.TargetUserId)
			.GreaterThan(0);

		RuleFor(request => request.FeedbackTag)
			.NotEmpty()
			.Must(feedbackTag => ValidTags.Contains(feedbackTag.Trim(), StringComparer.OrdinalIgnoreCase))
			.WithMessage("FeedbackTag must be one of: Good, Hesitated, Mistake, Unclear Pronunciation.");
	}
}
