namespace GoWithFlow.Application.Common;

/// <summary>
/// Well-known control signals shared between <c>ILiveSessionService.ShiftTurnAsync</c> and the
/// SignalR <c>LiveSessionHub.CompleteTurn</c> handler.
/// </summary>
/// <remarks>
/// History: the last-turn auto-completion used to break because the service placed the
/// "no further turns" marker in <c>ApiResponse.Errors</c> while the hub checked
/// <c>ApiResponse.Message</c>. The two drifted and the final turn threw a HubException instead
/// of completing the session. This constant makes the signal a single, shared value carried in
/// <c>ApiResponse.Message</c> so the two sides can never disagree again.
/// </remarks>
public static class TurnShiftSignals
{
	/// <summary>
	/// Placed in <c>ApiResponse.Message</c> by <c>ShiftTurnAsync</c> when the final turn has been
	/// completed (no next turn exists AND there was no error creating one). The hub keys session
	/// auto-completion on this exact value via <see cref="string.Contains(string, System.StringComparison)"/>.
	/// </summary>
	public const string SessionComplete = "No further turns remain";
}
