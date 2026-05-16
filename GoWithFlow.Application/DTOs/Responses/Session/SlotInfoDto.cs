namespace GoWithFlow.Application.DTOs.Responses.Session;

public sealed class SlotInfoDto
{
	public byte SlotIndex { get; set; }

	public string SlotName { get; set; } = string.Empty;

	public bool IsOccupied { get; set; }

	public string? UserFullName { get; set; }

	public bool IsReady { get; set; }
}
