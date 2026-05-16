namespace GoWithFlow.Application.DTOs.Requests;

public sealed class VerifyOtpRequestDto
{
	public string MobileNumber { get; set; } = string.Empty;

	public string OtpCode { get; set; } = string.Empty;
}
