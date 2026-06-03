using FluentValidation;
using GoWithFlow.Application.DTOs.Requests.Session;

namespace GoWithFlow.Application.Validators;

public sealed class CreateSessionRequestValidator : AbstractValidator<CreateSessionRequestDto>
{
	private static readonly int[] ValidDurations = { 15, 30, 45, 60, 90 };
	private static readonly int[] ValidExpiryMinutes = { 60, 120, 360, 1440 };

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
	}
}
