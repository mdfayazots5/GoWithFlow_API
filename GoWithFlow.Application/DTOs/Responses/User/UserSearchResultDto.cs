namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class UserSearchResultDto
{
	public long UserId { get; set; }
	public string FullName { get; set; } = string.Empty;
	public string? AvatarUrl { get; set; }
}
