using System.Security.Cryptography;
using AutoMapper;
using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests;
using GoWithFlow.Application.DTOs.Responses;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Application.Interfaces.Services;
using GoWithFlow.Application.Settings;
using GoWithFlow.Domain.Entities;
using GoWithFlow.Domain.Enums;
using Microsoft.Extensions.Options;

namespace GoWithFlow.Application.Services;

public sealed class AuthService : IAuthService
{
	private readonly IMapper _mapper;
	private readonly IUserRepository _userRepository;
	private readonly IRefreshTokenRepository _refreshTokenRepository;
	private readonly IJwtService _jwtService;
	private readonly JwtSettings _jwtSettings;

	public AuthService(
		IMapper mapper,
		IUserRepository userRepository,
		IRefreshTokenRepository refreshTokenRepository,
		IJwtService jwtService,
		IOptions<JwtSettings> jwtOptions)
	{
		_mapper = mapper;
		_userRepository = userRepository;
		_refreshTokenRepository = refreshTokenRepository;
		_jwtService = jwtService;
		_jwtSettings = jwtOptions.Value;
	}

	public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default)
	{
		var user = await _userRepository.GetByMobileNumberAsync(dto.MobileNumber, cancellationToken);

		if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash) || !VerifyPassword(dto.Password, user.PasswordHash))
		{
			return ApiResponse<AuthResponseDto>.FailureResult(new[] { "Invalid mobile number or password." }, "Authentication failed.");
		}

		if (!user.IsActive)
		{
			return ApiResponse<AuthResponseDto>.FailureResult(new[] { "Account is inactive." }, "Authentication failed.");
		}

		await _userRepository.UpdateLastLoginAsync(user.UserId, user.FullName, "127.0.0.1", cancellationToken);

		var authResponse = await CreateAuthResponseAsync(user, cancellationToken);

		return ApiResponse<AuthResponseDto>.SuccessResult(authResponse, "Login successful.");
	}

	public async Task<ApiResponse<UserProfileResponseDto>> RegisterAsync(RegisterRequestDto dto, CancellationToken cancellationToken = default)
	{
		var existingUser = await _userRepository.GetByMobileNumberAsync(dto.MobileNumber, cancellationToken);

		if (existingUser is not null)
		{
			return ApiResponse<UserProfileResponseDto>.FailureResult(new[] { "Mobile number is already registered." }, "Registration failed.");
		}

		var user = new User
		{
			FullName = dto.FullName.Trim(),
			MobileNumber = dto.MobileNumber.Trim(),
			Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
			PasswordHash = HashPassword(dto.Password),
			AgeGroup = MapAgeGroup(dto.AgeGroup),
			PreferredHintLanguage = dto.PreferredHintLanguage.ToString(),
			AvatarUrl = string.IsNullOrWhiteSpace(dto.AvatarUrl) ? null : dto.AvatarUrl.Trim(),
			GroupCode = null,
			Role = UserRoleType.USER.ToString(),
			CreatedBy = dto.FullName.Trim(),
			IPAddress = "127.0.0.1"
		};

		var userId = await _userRepository.InsertUserAsync(user, cancellationToken);
		var createdUser = await _userRepository.GetByUserIdAsync(userId, cancellationToken);

		if (createdUser is null)
		{
			return ApiResponse<UserProfileResponseDto>.FailureResult(new[] { "User registration could not be completed." }, "Registration failed.");
		}

		var response = _mapper.Map<UserProfileResponseDto>(createdUser);

		return ApiResponse<UserProfileResponseDto>.SuccessResult(response, "User registered successfully.");
	}

	public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken = default)
	{
		var existingRefreshToken = await _refreshTokenRepository.GetByTokenAsync(dto.RefreshToken, cancellationToken);

		if (existingRefreshToken is null || existingRefreshToken.ExpiresAt <= DateTime.UtcNow || existingRefreshToken.IsRevoked)
		{
			return ApiResponse<AuthResponseDto>.FailureResult(new[] { "Invalid or expired refresh token." }, "Refresh token validation failed.");
		}

		var user = await _userRepository.GetByUserIdAsync(existingRefreshToken.UserId, cancellationToken);

		if (user is null)
		{
			return ApiResponse<AuthResponseDto>.FailureResult(new[] { "User account was not found." }, "Refresh token validation failed.");
		}

		await _refreshTokenRepository.RevokeRefreshTokenAsync(dto.RefreshToken, user.FullName, "127.0.0.1", cancellationToken);

		var authResponse = await CreateAuthResponseAsync(user, cancellationToken);

		return ApiResponse<AuthResponseDto>.SuccessResult(authResponse, "Access token refreshed successfully.");
	}

	public async Task<ApiResponse<bool>> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(refreshToken))
		{
			return ApiResponse<bool>.FailureResult(new[] { "Refresh token is required." }, "Logout failed.");
		}

		await _refreshTokenRepository.RevokeRefreshTokenAsync(refreshToken, "System", "127.0.0.1", cancellationToken);

		return ApiResponse<bool>.SuccessResult(true, "Logout completed successfully.");
	}

	private async Task<AuthResponseDto> CreateAuthResponseAsync(User user, CancellationToken cancellationToken)
	{
		var accessToken = _jwtService.GenerateAccessToken(user);
		var refreshTokenValue = _jwtService.GenerateRefreshToken();

		var refreshToken = new RefreshToken
		{
			UserId = user.UserId,
			Token = refreshTokenValue,
			ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
			DeviceInfo = "Web",
			CreatedBy = user.FullName,
			IPAddress = "127.0.0.1"
		};

		await _refreshTokenRepository.InsertRefreshTokenAsync(refreshToken, cancellationToken);

		return new AuthResponseDto
		{
			AccessToken = accessToken,
			RefreshToken = refreshTokenValue,
			ExpiresIn = _jwtSettings.AccessTokenExpiryMinutes * 60,
			UserId = user.UserId,
			FullName = user.FullName,
			Role = user.Role,
			AvatarUrl = user.AvatarUrl
		};
	}

	private static string HashPassword(string password)
	{
		byte[] salt = RandomNumberGenerator.GetBytes(16);
		byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
		return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
	}

	private static bool VerifyPassword(string password, string storedHash)
	{
		var parts = storedHash.Split(':');
		if (parts.Length != 2) return false;
		byte[] salt = Convert.FromBase64String(parts[0]);
		byte[] expectedHash = Convert.FromBase64String(parts[1]);
		byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
		return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
	}

	private static string MapAgeGroup(AgeGroupType ageGroup)
	{
		return ageGroup switch
		{
			AgeGroupType.Child => "Child (6-12)",
			AgeGroupType.Teen => "Teen (13-17)",
			AgeGroupType.Adult => "Adult (18+)",
			_ => throw new ArgumentOutOfRangeException(nameof(ageGroup), ageGroup, "Unsupported age group.")
		};
	}
}
