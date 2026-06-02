namespace GoWithFlow.Application.DTOs.Requests.Session;

public sealed class RespondToInvitationRequestDto
{
	/// <summary>ACCEPTED or DECLINED</summary>
	public string Status { get; set; } = string.Empty;
}
