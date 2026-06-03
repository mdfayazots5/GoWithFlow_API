namespace GoWithFlow.Application.DTOs.Responses.Admin;

public sealed class AdminUserListResponseDto
{
	public long UserId { get; set; }

	public string FullName { get; set; } = string.Empty;

	public string MobileNumber { get; set; } = string.Empty;

	public string AgeGroup { get; set; } = string.Empty;

	public int TotalSessionsPlayed { get; set; }

	public int DailyStreakCount { get; set; }

	public DateTime? LastLoginDate { get; set; }

	public bool IsActive { get; set; }

	/// <summary>Presigned R2 URL resolved per-request. Null when no avatar is set.</summary>
	public string? AvatarUrl { get; set; }
}
