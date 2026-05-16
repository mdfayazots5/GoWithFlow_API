using GoWithFlow.Application.Common;
using GoWithFlow.Domain.Entities;

namespace GoWithFlow.Application.Interfaces.Repositories;

public interface IOtpRepository : IGenericRepository<OtpVerification>
{
	Task<long> InsertOtpAsync(OtpVerification otpVerification, CancellationToken cancellationToken = default);

	Task<OtpVerificationResult> VerifyOtpAsync(string mobileNumber, string otpCode, string updatedBy, string ipAddress, CancellationToken cancellationToken = default);
}
