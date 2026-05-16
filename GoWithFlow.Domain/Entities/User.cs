namespace GoWithFlow.Domain.Entities;

public sealed class User : BaseAuditEntity
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

	public string Role { get; set; } = "USER";

	public int DailyStreakCount { get; set; }

	public int TotalSessionsPlayed { get; set; }

	public DateTime? LastLoginDate { get; set; }

	public bool IsActive { get; set; } = true;

	public DateTime RegistrationDate { get; set; }

	public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

	public ICollection<UserStreak> UserStreaks { get; set; } = new List<UserStreak>();

	public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
}
