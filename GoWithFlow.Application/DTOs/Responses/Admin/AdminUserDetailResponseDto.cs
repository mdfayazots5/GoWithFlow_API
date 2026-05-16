namespace GoWithFlow.Application.DTOs.Responses.Admin;

public sealed class AdminUserDetailResponseDto
{
	public long UserId { get; set; }

	public string FullName { get; set; } = string.Empty;

	public string MobileNumber { get; set; } = string.Empty;

	public string? Email { get; set; }

	public string? PasswordHash { get; set; }

	public string AgeGroup { get; set; } = string.Empty;

	public string PreferredHintLanguage { get; set; } = string.Empty;

	public string? AvatarUrl { get; set; }

	public string? GroupCode { get; set; }

	public string Role { get; set; } = string.Empty;

	public int DailyStreakCount { get; set; }

	public int TotalSessionsPlayed { get; set; }

	public DateTime? LastLoginDate { get; set; }

	public bool IsActive { get; set; }

	public DateTime RegistrationDate { get; set; }

	public string? Tag { get; set; }

	public string? Comments { get; set; }

	public int SortOrder { get; set; }

	public string IPAddress { get; set; } = string.Empty;

	public string CreatedBy { get; set; } = string.Empty;

	public DateTime DateCreated { get; set; }

	public string? UpdatedBy { get; set; }

	public DateTime? LastUpdated { get; set; }

	public string? DeletedBy { get; set; }

	public DateTime? DateDeleted { get; set; }

	public bool IsDeleted { get; set; }

	public decimal AvgFluencyScore { get; set; }

	public string MostCommonMistakeType { get; set; } = string.Empty;

	public List<SessionSummaryDto> RecentSessions { get; set; } = new();
}
