namespace GoWithFlow.Application.DTOs.Requests.LiveSession;

public sealed class ListenerFeedbackRequestDto
{
	public long SessionId { get; set; }

	public int TurnIndex { get; set; }

	public long TargetUserId { get; set; }

	public string FeedbackTag { get; set; } = string.Empty;
}
