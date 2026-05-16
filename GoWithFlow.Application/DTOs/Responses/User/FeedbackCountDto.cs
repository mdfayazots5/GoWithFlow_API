namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class FeedbackCountDto
{
	public string FeedbackTag { get; set; } = string.Empty;

	public int Count { get; set; }
}
