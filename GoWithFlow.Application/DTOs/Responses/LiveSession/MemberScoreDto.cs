namespace GoWithFlow.Application.DTOs.Responses.LiveSession;

public sealed class MemberScoreDto
{
	public long UserId { get; set; }

	public string FullName { get; set; } = string.Empty;

	public decimal FluencyScore { get; set; }

	public decimal ConfidenceScore { get; set; }

	public int MistakeCount { get; set; }

	public decimal ListenerRating { get; set; }
}
