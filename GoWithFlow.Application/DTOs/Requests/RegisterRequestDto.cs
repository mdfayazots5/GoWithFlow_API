using GoWithFlow.Domain.Enums;

namespace GoWithFlow.Application.DTOs.Requests;

public sealed class RegisterRequestDto
{
	public string FullName { get; set; } = string.Empty;

	public string MobileNumber { get; set; } = string.Empty;

	public string? Email { get; set; }

	public AgeGroupType AgeGroup { get; set; }

	public PreferredHintLanguageType PreferredHintLanguage { get; set; }

	public string? AvatarUrl { get; set; }
}
