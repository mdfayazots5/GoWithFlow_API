namespace GoWithFlow.Domain.Entities;

public sealed class Session : BaseAuditEntity
{
	public long SessionId { get; set; }

	public string SessionName { get; set; } = string.Empty;

	public string JoinCode { get; set; } = string.Empty;

	public string SessionMode { get; set; } = string.Empty;

	public byte MaxMembers { get; set; }

	public int SessionDuration { get; set; }

	public long HostUserId { get; set; }

	public long ScriptId { get; set; }

	public string Status { get; set; } = string.Empty;

	public int RoomExpiryMinutes { get; set; }

	public DateTime? RoomExpiresAt { get; set; }

	public DateTime? StartedDate { get; set; }

	public DateTime? EndedDate { get; set; }

	public int? ActualDurationSec { get; set; }

	public User? Host { get; set; }

	public Script? Script { get; set; }

	public ICollection<SessionMember> Members { get; set; } = new List<SessionMember>();
}
