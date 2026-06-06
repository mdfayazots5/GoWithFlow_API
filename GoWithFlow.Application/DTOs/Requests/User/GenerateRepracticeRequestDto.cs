namespace GoWithFlow.Application.DTOs.Requests.User;

public sealed class GenerateRepracticeRequestDto
{
	/// <summary>
	/// The session whose unresolved mistakes seed the repractice round. When
	/// <see cref="IncludeAllSessions"/> is true this is ignored for mistake selection
	/// (all sessions are pulled) and may be 0 — the service derives a valid FK anchor
	/// from the loaded mistakes.
	/// </summary>
	public long SourceSessionId { get; set; }

	/// <summary>
	/// When true ("Practice All Mistakes"), pulls every unresolved mistake for the user
	/// across all sessions. When false (per-row "practice"), restricts to <see cref="SourceSessionId"/>.
	/// </summary>
	public bool IncludeAllSessions { get; set; }
}
