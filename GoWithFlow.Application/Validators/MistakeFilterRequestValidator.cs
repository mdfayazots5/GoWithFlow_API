using FluentValidation;
using GoWithFlow.Application.DTOs.Requests.User;
using GoWithFlow.Domain.Enums;

namespace GoWithFlow.Application.Validators;

public sealed class MistakeFilterRequestValidator : AbstractValidator<MistakeFilterRequestDto>
{
	public MistakeFilterRequestValidator()
	{
		RuleFor(request => request.PageNumber)
			.GreaterThan(0);

		RuleFor(request => request.PageSize)
			.InclusiveBetween(1, 100);

		RuleFor(request => request.MistakeType)
			.Must(BeValidMistakeType)
			.When(request => string.IsNullOrWhiteSpace(request.MistakeType) == false);
	}

	private static bool BeValidMistakeType(string? mistakeType)
	{
		return Enum.TryParse<MistakeTypeCode>(mistakeType, true, out _);
	}
}
