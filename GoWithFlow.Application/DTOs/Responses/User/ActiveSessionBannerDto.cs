namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class ActiveSessionBannerDto
{
	public long SessionId { get; set; }

	public string SessionName { get; set; } = string.Empty;

	public string Status { get; set; } = string.Empty;

	public string JoinCode { get; set; } = string.Empty;
}
