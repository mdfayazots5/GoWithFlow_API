namespace GoWithFlow.Application.DTOs.Responses.Script;

public sealed class ScriptUploadResponseDto
{
	public long ScriptId { get; set; }

	public string ScriptTitle { get; set; } = string.Empty;

	public int Version { get; set; }

	public int UtteranceCount { get; set; }
}
