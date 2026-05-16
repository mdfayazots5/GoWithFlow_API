namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class SessionHeaderDto
{
	public string SessionName { get; set; } = string.Empty;

	public string SessionMode { get; set; } = string.Empty;

	public DateTime SessionDate { get; set; }

	public int Duration { get; set; }

	public string ScriptTitle { get; set; } = string.Empty;

	public int MemberCount { get; set; }
}
