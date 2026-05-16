namespace GoWithFlow.Application.DTOs.Responses.Script;

public sealed class ScriptVersionResponseDto
{
	public long ScriptVersionId { get; set; }

	public long ScriptId { get; set; }

	public int VersionNumber { get; set; }

	public string? VersionNotes { get; set; }

	public long UploadedByUserId { get; set; }

	public DateTime UploadedDate { get; set; }
}
