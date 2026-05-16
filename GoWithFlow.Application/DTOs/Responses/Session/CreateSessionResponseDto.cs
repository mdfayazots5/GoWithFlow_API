namespace GoWithFlow.Application.DTOs.Responses.Session;

public sealed class CreateSessionResponseDto
{
	public long SessionId { get; set; }

	public string SessionName { get; set; } = string.Empty;

	public string JoinCode { get; set; } = string.Empty;

	public string Status { get; set; } = string.Empty;

	public string ScriptTitle { get; set; } = string.Empty;
}
