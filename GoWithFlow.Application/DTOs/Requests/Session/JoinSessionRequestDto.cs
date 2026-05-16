namespace GoWithFlow.Application.DTOs.Requests.Session;

public sealed class JoinSessionRequestDto
{
	public string JoinCode { get; set; } = string.Empty;

	public byte SlotIndex { get; set; }
}
