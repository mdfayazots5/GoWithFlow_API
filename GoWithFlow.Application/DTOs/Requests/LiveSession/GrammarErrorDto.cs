namespace GoWithFlow.Application.DTOs.Requests.LiveSession;

public sealed class GrammarErrorDto
{
	public string ExpectedPhrase { get; set; } = string.Empty;

	public string SpokenPhrase { get; set; } = string.Empty;

	public string ErrorType { get; set; } = string.Empty;

	public int Position { get; set; }
}
