namespace GoWithFlow.Application.DTOs.Requests.Session;

public sealed class SendInvitationsRequestDto
{
	public long SessionId { get; set; }

	public List<InvitationSlotAssignmentDto> Assignments { get; set; } = new();
}

public sealed class InvitationSlotAssignmentDto
{
	public long UserId { get; set; }

	public byte SlotIndex { get; set; }
}
