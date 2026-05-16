namespace GoWithFlow.Application.DTOs.Requests.Script;

public sealed class ScriptStatusUpdateRequestDto
{
	public long ScriptId { get; set; }

	public bool IsActive { get; set; }
}
