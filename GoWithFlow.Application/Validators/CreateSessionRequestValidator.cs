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

		RuleFor(request => request.SessionMode)
			.IsInEnum();

		RuleFor(request => request.MaxMembers)
			.InclusiveBetween((byte)2, (byte)5);

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
