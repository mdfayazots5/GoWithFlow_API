namespace GoWithFlow.Domain.Entities;

public sealed class RefreshToken : BaseAuditEntity
{
	public long RefreshTokenId { get; set; }

	public long UserId { get; set; }

	public string Token { get; set; } = string.Empty;

	public DateTime ExpiresAt { get; set; }

	public bool IsRevoked { get; set; }

	public DateTime? RevokedAt { get; set; }

	public string? DeviceInfo { get; set; }

	public User? User { get; set; }
}
