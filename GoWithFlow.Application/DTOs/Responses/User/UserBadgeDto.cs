namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class UserBadgeDto
{
	public string BadgeCode { get; set; } = string.Empty;

	public string BadgeName { get; set; } = string.Empty;

	public DateTime EarnedDate { get; set; }

	public bool IsEarned { get; set; } = true;
}
