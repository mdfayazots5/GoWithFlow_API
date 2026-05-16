namespace GoWithFlow.Domain.Entities;

public sealed class OtpVerification : BaseAuditEntity
{
	public long OtpVerificationId { get; set; }

	public string MobileNumber { get; set; } = string.Empty;

	public string OtpCode { get; set; } = string.Empty;

	public DateTime ExpiresAt { get; set; }

	public bool IsVerified { get; set; }

	public DateTime? VerifiedAt { get; set; }

	public int AttemptCount { get; set; }
}
