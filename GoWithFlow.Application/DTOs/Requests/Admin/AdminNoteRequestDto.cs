namespace GoWithFlow.Application.DTOs.Requests.Admin;

public sealed class AdminNoteRequestDto
{
	public long TargetUserId { get; set; }

	public string NoteText { get; set; } = string.Empty;
}
