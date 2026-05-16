using GoWithFlow.Domain.Enums;

namespace GoWithFlow.Application.DTOs.Requests.Session;

public sealed class CreateSessionRequestDto
{
	public string SessionName { get; set; } = string.Empty;

	public SessionModeType SessionMode { get; set; }

	public byte MaxMembers { get; set; }

	public int SessionDuration { get; set; }

	public long ScriptId { get; set; }

	public int RoomExpiryMinutes { get; set; }
}
