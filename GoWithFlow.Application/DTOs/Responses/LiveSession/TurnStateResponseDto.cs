using GoWithFlow.Application.DTOs.Responses.Script;

namespace GoWithFlow.Application.DTOs.Responses.LiveSession;

public sealed class TurnStateResponseDto
{
	public long SessionId { get; set; }

	public int TurnIndex { get; set; }

	public int TotalTurns { get; set; }

	public long ActiveMemberId { get; set; }

	public string ActiveMemberName { get; set; } = string.Empty;

	public byte ActiveSlotIndex { get; set; }

	public UtteranceResponseDto Utterance { get; set; } = new();

	public bool ReReadAllowed { get; set; }

	public int ReReadCount { get; set; }

	public int MaxReReads { get; set; }

	/// <summary>
	/// True when the active speaker is in a facilitator role (Interviewer, Tutor, Coach).
	/// Facilitator turns display text for read-aloud only — no voice analysis, no scoring.
	/// </summary>
	public bool IsFacilitatorTurn { get; set; }
}
