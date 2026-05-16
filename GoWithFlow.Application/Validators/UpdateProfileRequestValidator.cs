using FluentValidation;
using GoWithFlow.Application.DTOs.Requests.User;

namespace GoWithFlow.Application.Validators;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequestDto>
{
	public UpdateProfileRequestValidator()
	{
		RuleFor(request => request.FullName)
			.NotEmpty()
			.MinimumLength(2)
			.MaximumLength(60);

		RuleFor(request => request.Email)
			.EmailAddress()
			.When(request => string.IsNullOrWhiteSpace(request.Email) == false);

		RuleFor(request => request.AgeGroup)
			.IsInEnum();

		RuleFor(request => request.PreferredHintLanguage)
			.IsInEnum();

		RuleFor(request => request.AvatarUrl)
			.MaximumLength(256)
			.When(request => string.IsNullOrWhiteSpace(request.AvatarUrl) == false);
	}
}
