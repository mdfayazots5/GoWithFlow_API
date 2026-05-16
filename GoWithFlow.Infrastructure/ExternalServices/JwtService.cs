using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GoWithFlow.Application.Interfaces.Services;
using GoWithFlow.Application.Settings;
using GoWithFlow.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GoWithFlow.Infrastructure.ExternalServices;

public sealed class JwtService : IJwtService
{
	private readonly JwtSettings _jwtSettings;
	private readonly byte[] _secretKeyBytes;

	public JwtService(IOptions<JwtSettings> jwtOptions)
	{
		_jwtSettings = jwtOptions.Value;
		_secretKeyBytes = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);
	}

	public string GenerateAccessToken(User user)
	{
		var claims = new List<Claim>
		{
			new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
			new("UserId", user.UserId.ToString()),
			new("FullName", user.FullName),
			new("Role", user.Role),
			new("MobileNumber", user.MobileNumber),
			new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
			new(ClaimTypes.Name, user.FullName),
			new(ClaimTypes.Role, user.Role)
		};

		var signingCredentials = new SigningCredentials(
			new SymmetricSecurityKey(_secretKeyBytes),
			SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			issuer: _jwtSettings.Issuer,
			audience: _jwtSettings.Audience,
			claims: claims,
			expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
			signingCredentials: signingCredentials);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}

	public string GenerateRefreshToken()
	{
		var randomBytes = RandomNumberGenerator.GetBytes(64);
		return Convert.ToBase64String(randomBytes);
	}

	public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
	{
		var tokenValidationParameters = BuildTokenValidationParameters(false);
		var tokenHandler = new JwtSecurityTokenHandler();

		try
		{
			return tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
		}
		catch
		{
			return null;
		}
	}

	public bool ValidateToken(string token)
	{
		var tokenValidationParameters = BuildTokenValidationParameters(true);
		var tokenHandler = new JwtSecurityTokenHandler();

		try
		{
			tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private TokenValidationParameters BuildTokenValidationParameters(bool validateLifetime)
	{
		return new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateIssuerSigningKey = true,
			ValidateLifetime = validateLifetime,
			ValidIssuer = _jwtSettings.Issuer,
			ValidAudience = _jwtSettings.Audience,
			IssuerSigningKey = new SymmetricSecurityKey(_secretKeyBytes),
			ClockSkew = TimeSpan.Zero
		};
	}
}
