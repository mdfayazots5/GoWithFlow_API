namespace GoWithFlow.Application.Settings;

public sealed class OtpSettings
{
	public int ExpiryMinutes { get; set; }

	public int MaxAttempts { get; set; }
}
