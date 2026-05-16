namespace GoWithFlow.Domain.Entities;

public sealed class ScriptVersion : BaseAuditEntity
{
	public long ScriptVersionId { get; set; }

	public long ScriptId { get; set; }

	public int VersionNumber { get; set; }

	public string? VersionNotes { get; set; }

	public long UploadedByUserId { get; set; }

	public DateTime UploadedDate { get; set; }
}
