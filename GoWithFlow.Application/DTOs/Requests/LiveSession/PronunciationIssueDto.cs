namespace GoWithFlow.Application.DTOs.Requests.LiveSession;

public sealed class PronunciationIssueDto
{
	public string Word { get; set; } = string.Empty;

	public string ExpectedPhonetic { get; set; } = string.Empty;

	public string IssueNote { get; set; } = string.Empty;
}
