namespace GoWithFlow.Domain.Entities;

public sealed class AdminNote : BaseAuditEntity
{
	public long AdminNoteId { get; set; }

	public long AdminUserId { get; set; }

	public long TargetUserId { get; set; }

	public string NoteText { get; set; } = string.Empty;

	public DateTime NoteDate { get; set; }
}
