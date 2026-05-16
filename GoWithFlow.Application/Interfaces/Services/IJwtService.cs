using System.Security.Claims;
using GoWithFlow.Domain.Entities;

namespace GoWithFlow.Application.Interfaces.Services;

public interface IJwtService
{
	string GenerateAccessToken(User user);

	string GenerateRefreshToken();

	ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);

	bool ValidateToken(string token);
}
