namespace GoWithFlow.Application.DTOs.Responses;

public sealed class AuthResponseDto
{
	public string AccessToken { get; set; } = string.Empty;

	public string RefreshToken { get; set; } = string.Empty;

	public int ExpiresIn { get; set; }

	public long UserId { get; set; }

	public string FullName { get; set; } = string.Empty;

	public string Role { get; set; } = string.Empty;

	public string? AvatarUrl { get; set; }
}
