namespace GoWithFlow.Application.DTOs.Responses;

public sealed class UserProfileResponseDto
{
	public long UserId { get; set; }

	public string FullName { get; set; } = string.Empty;

	public string MobileNumber { get; set; } = string.Empty;

	public string? Email { get; set; }

	public string AgeGroup { get; set; } = string.Empty;

	public string PreferredHintLanguage { get; set; } = string.Empty;

	public string? AvatarUrl { get; set; }

	public string Role { get; set; } = string.Empty;

	public int DailyStreakCount { get; set; }

	public int TotalSessionsPlayed { get; set; }

	public int TotalSessions { get; set; }

	public decimal AvgFluencyScore { get; set; }

	public int TotalMistakesFixed { get; set; }

	public bool IsActive { get; set; }

	public DateTime RegistrationDate { get; set; }
}
