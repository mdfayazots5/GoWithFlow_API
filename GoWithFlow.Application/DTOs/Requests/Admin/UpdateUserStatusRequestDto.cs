namespace GoWithFlow.Application.DTOs.Requests.Admin;

public sealed class UpdateUserStatusRequestDto
{
	public long UserId { get; set; }

	public bool IsActive { get; set; }
}
