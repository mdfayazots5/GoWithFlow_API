namespace GoWithFlow.Application.DTOs.Requests.Session;

public sealed class UpdateReadyStatusRequestDto
{
	public long SessionId { get; set; }

	public bool IsReady { get; set; }
}
