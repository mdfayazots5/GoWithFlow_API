namespace GoWithFlow.Application.DTOs.Responses.Admin;

public sealed class AdminCreateUserResponseDto
{
	public long UserId { get; set; }

	public string FullName { get; set; } = string.Empty;

	public string MobileNumber { get; set; } = string.Empty;

	public string AgeGroup { get; set; } = string.Empty;

	public string Status { get; set; } = "ACTIVE";
}
