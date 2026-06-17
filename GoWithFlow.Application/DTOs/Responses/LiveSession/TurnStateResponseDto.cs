using GoWithFlow.Application.DTOs.Responses.Script;

namespace GoWithFlow.Application.DTOs.Responses.LiveSession;

public sealed class TurnStateResponseDto
{
	public long SessionId { get; set; }

	public int TurnIndex { get; set; }

	public int TotalTurns { get; set; }

	public long ActiveMemberId { get; set; }

	public string ActiveMemberName { get; set; } = string.Empty;

	public string? ActiveMemberAvatarUrl { get; set; }

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

	/// <summary>
	/// Phase 17 — true when the active slot is held by the AI Voice Participant. The client must
	/// narrate this turn via on-device TTS (no recognizer, no scoring) and then call AdvanceAiTurn.
	/// </summary>
	public bool IsAi { get; set; }

	// Phase 17 — session-level AI config, surfaced here so the narrating client has everything it
	// needs in the turn payload it already polls. Null on non-AI sessions.
	public string? AiVoiceGender { get; set; }

	public decimal? AiSpeechRate { get; set; }

	public int? AiQuestionDelaySec { get; set; }
}
