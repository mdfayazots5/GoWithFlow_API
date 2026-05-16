using System.Security.Cryptography;
using GoWithFlow.Application.Interfaces.Services;

namespace GoWithFlow.Infrastructure.ExternalServices;

public sealed class OtpService : IOtpService
{
	public string GenerateOtp()
	{
		return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
	}
}
