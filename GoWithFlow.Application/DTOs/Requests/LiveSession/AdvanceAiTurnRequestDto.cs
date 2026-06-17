namespace GoWithFlow.Application.DTOs.Requests.LiveSession;

/// <summary>
/// Phase 17 — body for advancing an AI-held turn. SessionId comes from the route; the caller is the
/// authenticated human member. TurnIndex must match the current active (AI) turn.
/// </summary>
public sealed class AdvanceAiTurnRequestDto
{
	public int TurnIndex { get; set; }
}
