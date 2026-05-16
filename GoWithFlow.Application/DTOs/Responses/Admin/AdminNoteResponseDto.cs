namespace GoWithFlow.Application.DTOs.Responses.Admin;

public sealed class AdminNoteResponseDto
{
	public long AdminNoteId { get; set; }

	public long AdminUserId { get; set; }

	public string AdminName { get; set; } = string.Empty;

	public string NoteText { get; set; } = string.Empty;

	public DateTime NoteDate { get; set; }
}
