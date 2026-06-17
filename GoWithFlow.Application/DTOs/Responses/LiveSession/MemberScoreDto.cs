namespace GoWithFlow.Application.DTOs.Responses.LiveSession;

public sealed class MemberScoreDto
{
	public long UserId { get; set; }

	public string FullName { get; set; } = string.Empty;

	public string? AvatarUrl { get; set; }

	public decimal FluencyScore { get; set; }

	public decimal ConfidenceScore { get; set; }

	public int MistakeCount { get; set; }

	public decimal ListenerRating { get; set; }

	/// <summary>
	/// True when this member played a facilitator role (Interviewer, Tutor, Coach).
	/// Facilitator members are excluded from the performance scoreboard.
	/// </summary>
	public bool IsFacilitator { get; set; }

	/// <summary>
	/// Phase 17 — true when this member is the AI Voice Participant. Excluded from the scored
	/// leaderboard (the AI narrates, it does not perform) and shown separately as an AI partner.
	/// </summary>
	public bool IsAi { get; set; }
}
