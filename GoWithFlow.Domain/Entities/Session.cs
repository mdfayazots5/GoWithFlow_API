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

	public DateTime? ScheduledAt { get; set; }

	// AI Voice Participant config (Phase 17). Null on sessions where AI was not enabled.
	public bool? AiEnabled { get; set; }

	public string? AiVoiceGender { get; set; }

	// Named Indian voice persona id (e.g. "aarav"). Added 2026-06-18; null on legacy/AI-off sessions.
	public string? AiVoiceName { get; set; }

	public decimal? AiSpeechRate { get; set; }

	public int? AiQuestionDelaySec { get; set; }

	public User? Host { get; set; }

	public Script? Script { get; set; }

	public ICollection<SessionMember> Members { get; set; } = new List<SessionMember>();

	public ICollection<SessionInvitation> Invitations { get; set; } = new List<SessionInvitation>();
}
