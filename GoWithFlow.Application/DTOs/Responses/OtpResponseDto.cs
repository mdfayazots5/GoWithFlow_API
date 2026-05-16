namespace GoWithFlow.Application.DTOs.Responses;

public sealed class OtpResponseDto
{
	public bool Sent { get; set; }

	public int ExpiresIn { get; set; }

	public string MobileNumber { get; set; } = string.Empty;

	public string? OtpCode { get; set; }
}
