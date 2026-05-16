using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.User;
using GoWithFlow.Application.DTOs.Responses;
using GoWithFlow.Application.DTOs.Responses.User;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Application.Interfaces.Services;
using GoWithFlow.Application.Settings;
using GoWithFlow.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GoWithFlow.Application.Services;

public sealed class UserService : IUserService
{
	private static readonly HashSet<string> AllowedAvatarExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".jpg",
		".jpeg",
		".png",
		".webp"
	};

	private readonly IUserRepository _userRepository;
	private readonly IWebHostEnvironment _webHostEnvironment;
	private readonly FileStorageSettings _fileStorageSettings;

	public UserService(
		IUserRepository userRepository,
		IWebHostEnvironment webHostEnvironment,
		IOptions<FileStorageSettings> fileStorageOptions)
	{
		_userRepository = userRepository;
		_webHostEnvironment = webHostEnvironment;
		_fileStorageSettings = fileStorageOptions.Value;
	}

	public async Task<ApiResponse<UserProfileResponseDto>> GetProfileAsync(long userId, CancellationToken cancellationToken = default)
	{
		if (userId <= 0)
		{
			return ApiResponse<UserProfileResponseDto>.FailureResult(new[] { "UserId must be greater than zero." }, "Validation failed.");
		}

		var profile = await _userRepository.GetUserProfileAsync(userId, cancellationToken);

		if (profile is null)
		{
			return ApiResponse<UserProfileResponseDto>.FailureResult(new[] { "User profile was not found." }, "User profile not found.");
		}

		return ApiResponse<UserProfileResponseDto>.SuccessResult(profile, "User profile retrieved successfully.");
	}

	public async Task<ApiResponse<UserProfileResponseDto>> UpdateProfileAsync(long userId, UpdateProfileRequestDto dto, CancellationToken cancellationToken = default)
	{
		if (userId <= 0)
		{
			return ApiResponse<UserProfileResponseDto>.FailureResult(new[] { "UserId must be greater than zero." }, "Validation failed.");
		}

		var existingUser = await _userRepository.GetByUserIdAsync(userId, cancellationToken);

		if (existingUser is null)
		{
			return ApiResponse<UserProfileResponseDto>.FailureResult(new[] { "User account was not found." }, "Profile update failed.");
		}

		await _userRepository.UpdateUserProfileAsync(
			userId,
			dto.FullName.Trim(),
			NormalizeNullableValue(dto.Email),
			MapAgeGroup(dto.AgeGroup),
			dto.PreferredHintLanguage.ToString(),
			NormalizeNullableValue(dto.AvatarUrl),
			existingUser.FullName,
			"127.0.0.1",
			cancellationToken);

		var updatedProfile = await _userRepository.GetUserProfileAsync(userId, cancellationToken);

		if (updatedProfile is null)
		{
			return ApiResponse<UserProfileResponseDto>.FailureResult(new[] { "Updated user profile could not be loaded." }, "Profile update failed.");
		}

		return ApiResponse<UserProfileResponseDto>.SuccessResult(updatedProfile, "User profile updated successfully.");
	}

	public async Task<ApiResponse<string>> UploadAvatarAsync(long userId, IFormFile file, CancellationToken cancellationToken = default)
	{
		if (userId <= 0)
		{
			return ApiResponse<string>.FailureResult(new[] { "UserId must be greater than zero." }, "Validation failed.");
		}

		if (file is null || file.Length <= 0)
		{
			return ApiResponse<string>.FailureResult(new[] { "Avatar file is required." }, "Validation failed.");
		}

		var maximumFileSizeInBytes = _fileStorageSettings.MaxFileSizeMB * 1024 * 1024;

		if (maximumFileSizeInBytes > 0 && file.Length > maximumFileSizeInBytes)
		{
			return ApiResponse<string>.FailureResult(new[] { $"Avatar file size cannot exceed {_fileStorageSettings.MaxFileSizeMB} MB." }, "Validation failed.");
		}

		var fileExtension = Path.GetExtension(file.FileName);

		if (AllowedAvatarExtensions.Contains(fileExtension) == false)
		{
			return ApiResponse<string>.FailureResult(new[] { "Only .jpg, .jpeg, .png, and .webp files are supported." }, "Validation failed.");
		}

		var existingUser = await _userRepository.GetByUserIdAsync(userId, cancellationToken);

		if (existingUser is null)
		{
			return ApiResponse<string>.FailureResult(new[] { "User account was not found." }, "Avatar upload failed.");
		}

		var webRootPath = string.IsNullOrWhiteSpace(_webHostEnvironment.WebRootPath)
			? Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot")
			: _webHostEnvironment.WebRootPath;
		var normalizedAvatarPath = (_fileStorageSettings.AvatarPath ?? "wwwroot/avatars/").Trim();
		var relativeAvatarPath = normalizedAvatarPath.TrimStart('~', '/', '\\').Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
		var avatarDirectoryPath = Path.IsPathRooted(relativeAvatarPath)
			? relativeAvatarPath
			: Path.Combine(_webHostEnvironment.ContentRootPath, relativeAvatarPath);
		Directory.CreateDirectory(avatarDirectoryPath);

		var fileName = $"{userId}_{Guid.NewGuid():N}{fileExtension.ToLowerInvariant()}";
		var physicalFilePath = Path.Combine(avatarDirectoryPath, fileName);

		await using (var fileStream = new FileStream(physicalFilePath, FileMode.Create))
		{
			await file.CopyToAsync(fileStream, cancellationToken);
		}

		var avatarUrl = $"/avatars/{fileName}";

		await _userRepository.UpdateUserProfileAsync(
			userId,
			existingUser.FullName,
			existingUser.Email,
			existingUser.AgeGroup,
			existingUser.PreferredHintLanguage,
			avatarUrl,
			existingUser.FullName,
			"127.0.0.1",
			cancellationToken);

		return ApiResponse<string>.SuccessResult(avatarUrl, "Avatar uploaded successfully.");
	}

	public async Task<ApiResponse<SessionDetailResponseDto>> GetSessionDetailAsync(long sessionId, long userId, CancellationToken cancellationToken = default)
	{
		if (sessionId <= 0 || userId <= 0)
		{
			return ApiResponse<SessionDetailResponseDto>.FailureResult(new[] { "SessionId and UserId must be greater than zero." }, "Validation failed.");
		}

		var sessionDetail = await _userRepository.GetSessionDetailAsync(sessionId, userId, cancellationToken);

		if (sessionDetail is null)
		{
			return ApiResponse<SessionDetailResponseDto>.FailureResult(new[] { "Session detail was not found." }, "Session detail not found.");
		}

		return ApiResponse<SessionDetailResponseDto>.SuccessResult(sessionDetail, "Session detail retrieved successfully.");
	}

	public async Task<ApiResponse<ImprovementDataResponseDto>> GetImprovementDataAsync(long userId, CancellationToken cancellationToken = default)
	{
		if (userId <= 0)
		{
			return ApiResponse<ImprovementDataResponseDto>.FailureResult(new[] { "UserId must be greater than zero." }, "Validation failed.");
		}

		var profile = await _userRepository.GetUserProfileAsync(userId, cancellationToken);

		if (profile is null)
		{
			return ApiResponse<ImprovementDataResponseDto>.FailureResult(new[] { "User profile was not found." }, "Improvement data not found.");
		}

		var recentSessions = await _userRepository.GetImprovementSessionsAsync(userId, cancellationToken);
		var weeklyScores = await _userRepository.GetWeeklyFluencyScoresAsync(userId, cancellationToken);
		var grammarProgress = await _userRepository.GetGrammarProgressAsync(userId, cancellationToken);
		var repracticeHistory = await _userRepository.GetRepracticeHistoryAsync(userId, 1, 10, cancellationToken);
		var badges = await _userRepository.GetBadgesAsync(userId, cancellationToken);
		var streakData = await _userRepository.GetStreakDataAsync(userId, cancellationToken);

		var response = new ImprovementDataResponseDto
		{
			RecentSessions = recentSessions,
			WeeklyScores = weeklyScores,
			GrammarProgress = grammarProgress,
			RepracticeHistory = repracticeHistory,
			BadgesEarned = badges,
			StatsHeader = new ImprovementStatsHeaderDto
			{
				SessionsCompleted = profile.TotalSessions,
				AvgScoreThisWeek = weeklyScores.FirstOrDefault()?.AvgFluencyScore ?? 0,
				MistakesResolved = profile.TotalMistakesFixed,
				CurrentStreak = streakData.CurrentStreak
			}
		};

		return ApiResponse<ImprovementDataResponseDto>.SuccessResult(response, "User improvement data retrieved successfully.");
	}

	public async Task<ApiResponse<StreakDataResponseDto>> GetStreakDataAsync(long userId, CancellationToken cancellationToken = default)
	{
		if (userId <= 0)
		{
			return ApiResponse<StreakDataResponseDto>.FailureResult(new[] { "UserId must be greater than zero." }, "Validation failed.");
		}

		var streakData = await _userRepository.GetStreakDataAsync(userId, cancellationToken);

		return ApiResponse<StreakDataResponseDto>.SuccessResult(streakData, "User streak data retrieved successfully.");
	}

	public async Task<ApiResponse<List<UserBadgeDto>>> GetBadgesAsync(long userId, CancellationToken cancellationToken = default)
	{
		if (userId <= 0)
		{
			return ApiResponse<List<UserBadgeDto>>.FailureResult(new[] { "UserId must be greater than zero." }, "Validation failed.");
		}

		var badges = await _userRepository.GetBadgesAsync(userId, cancellationToken);

		return ApiResponse<List<UserBadgeDto>>.SuccessResult(badges, "User badges retrieved successfully.");
	}

	public async Task UpsertStreakAsync(long userId, int practiceMinutes, CancellationToken cancellationToken = default)
	{
		if (userId <= 0)
		{
			return;
		}

		var existingUser = await _userRepository.GetByUserIdAsync(userId, cancellationToken);

		if (existingUser is null)
		{
			return;
		}

		await _userRepository.UpsertUserStreakAsync(userId, practiceMinutes, existingUser.FullName, "127.0.0.1", cancellationToken);
	}

	public async Task CheckAndAwardBadgesAsync(long userId, CancellationToken cancellationToken = default)
	{
		if (userId <= 0)
		{
			return;
		}

		var existingUser = await _userRepository.GetByUserIdAsync(userId, cancellationToken);

		if (existingUser is null)
		{
			return;
		}

		await _userRepository.CheckAndAwardBadgesAsync(userId, existingUser.FullName, "127.0.0.1", cancellationToken);
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

	private static string? NormalizeNullableValue(string? value)
	{
		return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
	}
}
