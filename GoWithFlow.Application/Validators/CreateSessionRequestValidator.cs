using FluentValidation;
using GoWithFlow.Application.DTOs.Requests.Session;

namespace GoWithFlow.Application.Validators;

public sealed class CreateSessionRequestValidator : AbstractValidator<CreateSessionRequestDto>
{
	private static readonly int[] ValidDurations = { 15, 30, 45, 60, 90 };
	private static readonly int[] ValidExpiryMinutes = { 60, 120, 360, 1440 };
	private static readonly string[] ValidVoiceGenders = { "Male", "Female" };
	private static readonly decimal[] ValidSpeechRates = { 0.75m, 1.00m, 1.25m };
	private static readonly int[] ValidQuestionDelays = { 0, 1, 2, 3, 5 };

	public CreateSessionRequestValidator()
	{
		RuleFor(request => request.SessionName)
			.NotEmpty()
			.MinimumLength(3)
			.MaximumLength(60);

		// SessionMode and MaxMembers are now derived from the script on the backend.
		// The frontend no longer sends them; validation is removed to avoid false 400s.

		RuleFor(request => request.SessionDuration)
			.Must(duration => ValidDurations.Contains(duration))
			.WithMessage("SessionDuration must be one of: 15, 30, 45, 60, 90.");

		RuleFor(request => request.ScriptId)
			.GreaterThan(0);

		RuleFor(request => request.RoomExpiryMinutes)
			.Must(expiryMinutes => ValidExpiryMinutes.Contains(expiryMinutes))
			.WithMessage("RoomExpiryMinutes must be one of: 60, 120, 360, 1440.");

		// AI Voice Participant (Phase 17) — the three settings are required only when enabled.
		When(request => request.AiEnabled, () =>
		{
			RuleFor(request => request.AiVoiceGender)
				.Must(gender => gender is not null && ValidVoiceGenders.Contains(gender))
				.WithMessage("AiVoiceGender must be one of: Male, Female.");

			RuleFor(request => request.AiSpeechRate)
				.Must(rate => rate is not null && ValidSpeechRates.Contains(rate.Value))
				.WithMessage("AiSpeechRate must be one of: 0.75, 1.00, 1.25.");

			RuleFor(request => request.AiQuestionDelaySec)
				.Must(delay => delay is not null && ValidQuestionDelays.Contains(delay.Value))
				.WithMessage("AiQuestionDelaySec must be one of: 0, 1, 2, 3, 5.");
		});
	}
}
