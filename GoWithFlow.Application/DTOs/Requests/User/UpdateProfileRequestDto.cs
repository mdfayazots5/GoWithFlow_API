using GoWithFlow.Domain.Enums;

namespace GoWithFlow.Application.DTOs.Requests.User;

public sealed class UpdateProfileRequestDto
{
	public string FullName { get; set; } = string.Empty;

	public string? Email { get; set; }

	public AgeGroupType AgeGroup { get; set; }

	public PreferredHintLanguageType PreferredHintLanguage { get; set; }

	public string? AvatarUrl { get; set; }
}
