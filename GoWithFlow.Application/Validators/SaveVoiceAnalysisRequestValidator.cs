using FluentValidation;
using GoWithFlow.Application.DTOs.Requests.LiveSession;

namespace GoWithFlow.Application.Validators;

public sealed class SaveVoiceAnalysisRequestValidator : AbstractValidator<SaveVoiceAnalysisRequestDto>
{
	public SaveVoiceAnalysisRequestValidator()
	{
		RuleFor(request => request.SessionId)
			.GreaterThan(0);

		RuleFor(request => request.TurnIndex)
			.GreaterThan(0);

		RuleFor(request => request.UtteranceId)
			.GreaterThan(0);

		RuleFor(request => request.TranscribedText)
			.MaximumLength(512)
			.When(request => string.IsNullOrWhiteSpace(request.TranscribedText) == false);

		RuleFor(request => request.ExpectedText)
			.NotEmpty()
			.MaximumLength(512);

		RuleFor(request => request.FluencyScore)
			.InclusiveBetween(0, 100);

		RuleFor(request => request.ConfidenceScore)
			.InclusiveBetween(0, 100);

		RuleFor(request => request.SpeakingSpeedWpm)
			.GreaterThanOrEqualTo(0);

		RuleFor(request => request.PauseCount)
			.GreaterThanOrEqualTo(0);

		RuleFor(request => request.OverallScore)
			.InclusiveBetween(0, 100);
	}
}
