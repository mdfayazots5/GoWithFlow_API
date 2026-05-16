namespace GoWithFlow.Application.Common;

public sealed class OtpVerificationResult
{
	public bool IsValid { get; init; }

	public long? UserId { get; init; }
}
