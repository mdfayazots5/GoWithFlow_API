namespace GoWithFlow.Application.DTOs.Responses.Session;

/// <summary>Returned to the host for the waiting-room view (per session).</summary>
public sealed class SessionInvitationDto
{
	public long InvitationId { get; set; }
	public long SessionId { get; set; }
	public long UserId { get; set; }
	public byte SlotIndex { get; set; }
	public string SlotName { get; set; } = string.Empty;
	public string Status { get; set; } = string.Empty;
	public DateTime SentAt { get; set; }
	public DateTime? RespondedAt { get; set; }
	public DateTime? ExpiresAt { get; set; }
	public string FullName { get; set; } = string.Empty;
	public string? AvatarUrl { get; set; }
}

/// <summary>Returned to the invitee for the dashboard inbox view.</summary>
public sealed class UserInvitationDto
{
	public long InvitationId { get; set; }
	public long SessionId { get; set; }
	public byte SlotIndex { get; set; }
	public string SlotName { get; set; } = string.Empty;
	public string Status { get; set; } = string.Empty;
	public DateTime SentAt { get; set; }
	public DateTime? ExpiresAt { get; set; }
	public string SessionName { get; set; } = string.Empty;
	public string SessionMode { get; set; } = string.Empty;
	public int SessionDuration { get; set; }
	public DateTime? ScheduledAt { get; set; }
	public string HostName { get; set; } = string.Empty;
	public string? HostAvatarUrl { get; set; }
}
