namespace GoWithFlow.Application.DTOs.Requests;

public sealed class LoginRequestDto
{
	public string MobileNumber { get; set; } = string.Empty;

	public string Password { get; set; } = string.Empty;
}
