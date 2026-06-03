using System.Security.Cryptography;
using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.Admin;
using GoWithFlow.Application.DTOs.Responses.Admin;
using GoWithFlow.Application.Helpers;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Application.Interfaces.Services;
using GoWithFlow.Application.Settings;
using GoWithFlow.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoWithFlow.Application.Services;

public sealed class AdminService : IAdminService
{
	private readonly IAdminRepository _adminRepository;
	private readonly IUserRepository _userRepository;
	private readonly IMemoryCache _memoryCache;
	private readonly IExcelExportService _excelExportService;
	private readonly IStorageService _storageService;
	private readonly CloudflareR2Settings _r2Settings;
	private readonly ILogger<AdminService> _logger;

	public AdminService(
		IAdminRepository adminRepository,
		IUserRepository userRepository,
		IMemoryCache memoryCache,
		IExcelExportService excelExportService,
		IStorageService storageService,
		IOptions<CloudflareR2Settings> r2Options,
		ILogger<AdminService> logger)
	{
		_adminRepository    = adminRepository;
		_userRepository     = userRepository;
		_memoryCache        = memoryCache;
		_excelExportService = excelExportService;
		_storageService     = storageService;
		_r2Settings         = r2Options.Value;
		_logger             = logger;
	}

	public async Task<ApiResponse<AdminDashboardResponseDto>> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
	{
		if (_memoryCache.TryGetValue(CacheKeys.AdminDashboardStats, out AdminDashboardResponseDto? cachedDashboardSummary) && cachedDashboardSummary is not null)
		{
			return ApiResponse<AdminDashboardResponseDto>.SuccessResult(cachedDashboardSummary, "Admin dashboard summary retrieved successfully.");
		}

		var dashboardSummary = await _adminRepository.GetDashboardSummaryAsync(cancellationToken);
		dashboardSummary.RecentActivities = await _adminRepository.GetRecentActivitiesAsync(10, cancellationToken);
		dashboardSummary.TopGrammarMistakes = await _adminRepository.GetTopGrammarMistakesAsync(5, cancellationToken);

		var avatarResolveTasks = dashboardSummary.RecentActivities
			.Where(a => IsR2Key(a.AvatarUrl))
			.Select(async a =>
			{
				a.AvatarUrl = await _storageService.GetPresignedUrlAsync(
					_r2Settings.Buckets.Avatars,
					a.AvatarUrl!,
					_r2Settings.PresignedUrlExpiryMinutes.Avatars,
					cancellationToken);
			});
		await Task.WhenAll(avatarResolveTasks);

		_memoryCache.Set(CacheKeys.AdminDashboardStats, dashboardSummary, TimeSpan.FromMinutes(2));

		return ApiResponse<AdminDashboardResponseDto>.SuccessResult(dashboardSummary, "Admin dashboard summary retrieved successfully.");
	}

	public async Task<ApiResponse<PagedResult<AdminUserListResponseDto>>> GetUsersAsync(AdminUserSearchRequestDto dto, CancellationToken cancellationToken = default)
	{
		NormalizeUserSearch(dto);

		var users = await _adminRepository.GetUsersAsync(dto, cancellationToken);

		// Resolve R2 keys to presigned URLs in parallel — one per user with an avatar
		var resolveTasks = users.Items
			.Where(u => IsR2Key(u.AvatarUrl))
			.Select(async u =>
			{
				u.AvatarUrl = await _storageService.GetPresignedUrlAsync(
					_r2Settings.Buckets.Avatars,
					u.AvatarUrl!,
					_r2Settings.PresignedUrlExpiryMinutes.Avatars,
					cancellationToken);
			});

		await Task.WhenAll(resolveTasks);

		return ApiResponse<PagedResult<AdminUserListResponseDto>>.SuccessResult(users, "Admin user list retrieved successfully.");
	}

	public async Task<ApiResponse<AdminUserDetailResponseDto>> GetUserDetailAsync(long userId, CancellationToken cancellationToken = default)
	{
		if (userId <= 0)
		{
			return ApiResponse<AdminUserDetailResponseDto>.FailureResult(new[] { "UserId must be greater than zero." }, "Validation failed.");
		}

		var userDetail = await _adminRepository.GetUserDetailAsync(userId, cancellationToken);

		if (userDetail is null)
		{
			return ApiResponse<AdminUserDetailResponseDto>.FailureResult(new[] { "User not found." }, "User detail not found.");
		}

		if (IsR2Key(userDetail.AvatarUrl))
		{
			userDetail.AvatarUrl = await _storageService.GetPresignedUrlAsync(
				_r2Settings.Buckets.Avatars,
				userDetail.AvatarUrl!,
				_r2Settings.PresignedUrlExpiryMinutes.Avatars,
				cancellationToken);
		}

		return ApiResponse<AdminUserDetailResponseDto>.SuccessResult(userDetail, "Admin user detail retrieved successfully.");
	}

	public async Task<ApiResponse<bool>> UpdateUserStatusAsync(UpdateUserStatusRequestDto dto, CancellationToken cancellationToken = default)
	{
		if (dto.UserId <= 0)
		{
			return ApiResponse<bool>.FailureResult(new[] { "UserId must be greater than zero." }, "Validation failed.");
		}

		var existingUser = await _userRepository.GetByUserIdAsync(dto.UserId, cancellationToken);

		if (existingUser is null)
		{
			return ApiResponse<bool>.FailureResult(new[] { "User not found." }, "User status update failed.");
		}

		await _adminRepository.UpdateUserStatusAsync(dto, "Admin", "127.0.0.1", cancellationToken);

		return ApiResponse<bool>.SuccessResult(true, "User active status updated successfully.");
	}

	public async Task<ApiResponse<AdminNoteResponseDto>> AddAdminNoteAsync(AdminNoteRequestDto dto, long adminUserId, CancellationToken cancellationToken = default)
	{
		if (adminUserId <= 0)
		{
			return ApiResponse<AdminNoteResponseDto>.FailureResult(new[] { "Admin user is invalid." }, "Admin note creation failed.");
		}

		if (dto.TargetUserId <= 0)
		{
			return ApiResponse<AdminNoteResponseDto>.FailureResult(new[] { "TargetUserId must be greater than zero." }, "Validation failed.");
		}

		if (string.IsNullOrWhiteSpace(dto.NoteText))
		{
			return ApiResponse<AdminNoteResponseDto>.FailureResult(new[] { "NoteText is required." }, "Validation failed.");
		}

		if (dto.NoteText.Length > 512)
		{
			return ApiResponse<AdminNoteResponseDto>.FailureResult(new[] { "NoteText cannot exceed 512 characters." }, "Validation failed.");
		}

		var adminUser = await _userRepository.GetByUserIdAsync(adminUserId, cancellationToken);
		var targetUser = await _userRepository.GetByUserIdAsync(dto.TargetUserId, cancellationToken);

		if (adminUser is null || targetUser is null)
		{
			return ApiResponse<AdminNoteResponseDto>.FailureResult(new[] { "Admin user or target user was not found." }, "Admin note creation failed.");
		}

		dto.NoteText = dto.NoteText.Trim();

		var adminNote = await _adminRepository.AddAdminNoteAsync(adminUserId, dto, adminUser.FullName, "127.0.0.1", cancellationToken);

		return ApiResponse<AdminNoteResponseDto>.SuccessResult(adminNote, "Admin note created successfully.");
	}

	public async Task<ApiResponse<List<AdminNoteResponseDto>>> GetAdminNotesByUserAsync(long targetUserId, CancellationToken cancellationToken = default)
	{
		if (targetUserId <= 0)
		{
			return ApiResponse<List<AdminNoteResponseDto>>.FailureResult(new[] { "TargetUserId must be greater than zero." }, "Validation failed.");
		}

		var adminNotes = await _adminRepository.GetAdminNotesByUserAsync(targetUserId, cancellationToken);

		return ApiResponse<List<AdminNoteResponseDto>>.SuccessResult(adminNotes, "Admin notes retrieved successfully.");
	}

	public async Task<ApiResponse<PagedResult<AdminReportSummaryDto>>> GetReportSummaryAsync(AdminReportFilterRequestDto dto, CancellationToken cancellationToken = default)
	{
		NormalizeReportFilter(dto);

		var reportSummary = await _adminRepository.GetReportSummaryAsync(dto, cancellationToken);

		var avatarTasks = reportSummary.Items
			.Where(r => IsR2Key(r.AvatarUrl))
			.Select(async r =>
			{
				r.AvatarUrl = await _storageService.GetPresignedUrlAsync(
					_r2Settings.Buckets.Avatars,
					r.AvatarUrl!,
					_r2Settings.PresignedUrlExpiryMinutes.Avatars,
					cancellationToken);
			});
		await Task.WhenAll(avatarTasks);

		return ApiResponse<PagedResult<AdminReportSummaryDto>>.SuccessResult(reportSummary, "Admin report summary retrieved successfully.");
	}

	public async Task<ApiResponse<AdminUserFullReportDto>> GetUserFullReportAsync(long userId, CancellationToken cancellationToken = default)
	{
		if (userId <= 0)
		{
			return ApiResponse<AdminUserFullReportDto>.FailureResult(new[] { "UserId must be greater than zero." }, "Validation failed.");
		}

		var report = await _adminRepository.GetUserFullReportAsync(userId, cancellationToken);

		if (report is null)
		{
			return ApiResponse<AdminUserFullReportDto>.FailureResult(new[] { "User report not found." }, "User report not found.");
		}

		if (IsR2Key(report.UserHeader.AvatarUrl))
		{
			report.UserHeader.AvatarUrl = await _storageService.GetPresignedUrlAsync(
				_r2Settings.Buckets.Avatars,
				report.UserHeader.AvatarUrl!,
				_r2Settings.PresignedUrlExpiryMinutes.Avatars,
				cancellationToken);
		}

		return ApiResponse<AdminUserFullReportDto>.SuccessResult(report, "Admin user full report retrieved successfully.");
	}

	public async Task<ApiResponse<string>> ExportReportsAsExcelAsync(AdminReportFilterRequestDto dto, CancellationToken cancellationToken = default)
	{
		NormalizeReportFilter(dto);

		var exportFilter = new AdminReportFilterRequestDto
		{
			FromDate   = dto.FromDate,
			ToDate     = dto.ToDate,
			UserId     = dto.UserId,
			PageNumber = 1,
			PageSize   = 10000
		};

		var workbookBytes = await _excelExportService.GenerateUserReportExcelAsync(exportFilter);

		// Determine requestedByUserId from filter or use 0 as default for key construction
		var requestedById = dto.UserId ?? 0;
		var objectKey     = StorageKeyBuilder.ReportExport(requestedById);
		var bucket        = _r2Settings.Buckets.Exports;

		await using var stream = new MemoryStream(workbookBytes);
		await _storageService.UploadAsync(stream, bucket, objectKey,
			"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", cancellationToken);

		var presignedUrl = await _storageService.GetPresignedUrlAsync(
			bucket, objectKey, _r2Settings.PresignedUrlExpiryMinutes.Exports, cancellationToken);

		return ApiResponse<string>.SuccessResult(presignedUrl, "Admin reports exported successfully.");
	}

	public async Task<ApiResponse<AdminCreateUserResponseDto>> CreateUserAsync(AdminCreateUserRequestDto dto, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(dto.FullName))
			return ApiResponse<AdminCreateUserResponseDto>.FailureResult(new[] { "Full name is required." }, "Validation failed.");

		if (string.IsNullOrWhiteSpace(dto.MobileNumber))
			return ApiResponse<AdminCreateUserResponseDto>.FailureResult(new[] { "Mobile number is required." }, "Validation failed.");

		if (string.IsNullOrWhiteSpace(dto.AgeGroup) || !IsValidAgeGroup(dto.AgeGroup))
			return ApiResponse<AdminCreateUserResponseDto>.FailureResult(new[] { "Age group must be one of: Child (6-12), Teen (13-17), Adult (18+)." }, "Validation failed.");

		if (string.IsNullOrWhiteSpace(dto.PreferredHintLanguage) || !IsValidHintLanguage(dto.PreferredHintLanguage))
			return ApiResponse<AdminCreateUserResponseDto>.FailureResult(new[] { "Preferred language must be one of: Telugu, Hindi, Tamil, Kannada, None." }, "Validation failed.");

		var existing = await _userRepository.GetByMobileNumberAsync(dto.MobileNumber.Trim(), cancellationToken);
		if (existing is not null)
			return ApiResponse<AdminCreateUserResponseDto>.FailureResult(new[] { "Mobile number is already registered." }, "User creation failed.");

		var user = new User
		{
			FullName = dto.FullName.Trim(),
			MobileNumber = dto.MobileNumber.Trim(),
			Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
			PasswordHash = string.IsNullOrWhiteSpace(dto.Password) ? null : HashPassword(dto.Password),
			AgeGroup = dto.AgeGroup.Trim(),
			PreferredHintLanguage = dto.PreferredHintLanguage.Trim(),
			Role = "USER",
			CreatedBy = "Admin",
			IPAddress = "127.0.0.1"
		};

		var userId = await _userRepository.InsertUserAsync(user, cancellationToken);

		string? avatarUrl = null;

		if (dto.Avatar is not null)
		{
			var result = await UploadAvatarInternalAsync(userId, dto.Avatar, cancellationToken);
			if (result is not null)
			{
				await _userRepository.UpdateAvatarUrlAsync(userId, result.Value.ObjectKey, cancellationToken);
				avatarUrl = result.Value.PresignedUrl;
			}
		}

		var response = new AdminCreateUserResponseDto
		{
			UserId = userId,
			FullName = user.FullName,
			MobileNumber = user.MobileNumber,
			AgeGroup = user.AgeGroup,
			Status = "ACTIVE",
			AvatarUrl = avatarUrl
		};

		return ApiResponse<AdminCreateUserResponseDto>.SuccessResult(response, "User created successfully.");
	}

	public async Task<ApiResponse<string?>> UpdateUserAsync(long userId, AdminUpdateUserRequestDto dto, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(dto.FullName))
			return ApiResponse<string?>.FailureResult(new[] { "Full name is required." }, "Validation failed.");

		if (string.IsNullOrWhiteSpace(dto.MobileNumber))
			return ApiResponse<string?>.FailureResult(new[] { "Mobile number is required." }, "Validation failed.");

		if (string.IsNullOrWhiteSpace(dto.AgeGroup) || !IsValidAgeGroup(dto.AgeGroup))
			return ApiResponse<string?>.FailureResult(new[] { "Age group must be one of: Child (6-12), Teen (13-17), Adult (18+)." }, "Validation failed.");

		if (string.IsNullOrWhiteSpace(dto.PreferredHintLanguage) || !IsValidHintLanguage(dto.PreferredHintLanguage))
			return ApiResponse<string?>.FailureResult(new[] { "Preferred language must be one of: Telugu, Hindi, Tamil, Kannada, None." }, "Validation failed.");

		var user = await _userRepository.GetByUserIdAsync(userId, cancellationToken);
		if (user is null)
			return ApiResponse<string?>.FailureResult(new[] { "User not found." }, "Update failed.");

		if (!string.Equals(user.MobileNumber, dto.MobileNumber.Trim(), StringComparison.OrdinalIgnoreCase))
		{
			var existing = await _userRepository.GetByMobileNumberAsync(dto.MobileNumber.Trim(), cancellationToken);
			if (existing is not null)
				return ApiResponse<string?>.FailureResult(new[] { "Mobile number is already registered." }, "Update failed.");
		}

		// Upload avatar if provided — get new R2 key and presigned URL
		string? avatarUrl   = null;
		string? newAvatarKey = null;

		if (dto.Avatar is not null)
		{
			var result = await UploadAvatarInternalAsync(userId, dto.Avatar, cancellationToken);
			if (result is not null)
			{
				newAvatarKey = result.Value.ObjectKey;
				avatarUrl    = result.Value.PresignedUrl;
			}
		}

		// Persist all changes via direct SQL — never via EF Core DbSet.Update() because
		// EF Core generates quoted PascalCase identifiers ("tblUser") that PostgreSQL
		// rejects as case-sensitive (DB has lowercase 'tbluser').
		await _userRepository.UpdateUserByAdminAsync(
			userId,
			dto.FullName.Trim(),
			dto.MobileNumber.Trim(),
			string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
			dto.AgeGroup.Trim(),
			dto.PreferredHintLanguage.Trim(),
			newAvatarKey ?? user.AvatarUrl,
			string.IsNullOrWhiteSpace(dto.Password) ? null : HashPassword(dto.Password),
			cancellationToken);

		return ApiResponse<string?>.SuccessResult(avatarUrl, "User updated successfully.");
	}

	private static string HashPassword(string password)
	{
		byte[] salt = RandomNumberGenerator.GetBytes(16);
		byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
		return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
	}

	private static bool IsValidAgeGroup(string value) =>
		value is "Child (6-12)" or "Teen (13-17)" or "Adult (18+)";

	private static bool IsR2Key(string? value) =>
		!string.IsNullOrEmpty(value)
		&& !value.StartsWith('/')
		&& !value.StartsWith("http", StringComparison.OrdinalIgnoreCase);

	private static readonly HashSet<string> AllowedAvatarExtensions =
		new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

	/// <summary>
	/// Uploads an avatar file to R2. Returns (ObjectKey, PresignedUrl) or null if extension not allowed.
	/// Caller must assign ObjectKey to user.AvatarUrl and call SaveChanges.
	/// </summary>
	private async Task<(string ObjectKey, string PresignedUrl)?> UploadAvatarInternalAsync(
		long userId, IFormFile file, CancellationToken cancellationToken)
	{
		var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
		if (!AllowedAvatarExtensions.Contains(extension))
			return null;

		var objectKey   = StorageKeyBuilder.UserAvatar(userId, extension.TrimStart('.'));
		var bucket      = _r2Settings.Buckets.Avatars;

		await using var stream = file.OpenReadStream();
		await _storageService.UploadAsync(stream, bucket, objectKey, file.ContentType, cancellationToken);

		var presignedUrl = await _storageService.GetPresignedUrlAsync(
			bucket, objectKey, _r2Settings.PresignedUrlExpiryMinutes.Avatars, cancellationToken);

		return (objectKey, presignedUrl);
	}

	public async Task<ApiResponse<PagedResult<AdminSessionHistoryItemDto>>> GetSessionHistoryAsync(
		AdminSessionHistoryFilterRequestDto dto,
		CancellationToken cancellationToken = default)
	{
		NormalizeSessionHistoryFilter(dto);
		var result = await _adminRepository.GetSessionHistoryAsync(dto, cancellationToken);
		return ApiResponse<PagedResult<AdminSessionHistoryItemDto>>.SuccessResult(result, "Session history retrieved successfully.");
	}

	public async Task<ApiResponse<CohortResponseDto>> CreateCohortAsync(CreateCohortRequestDto dto, string createdBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(dto.CohortName))
		{
			return ApiResponse<CohortResponseDto>.FailureResult(new[] { "CohortName is required." }, "Validation failed.");
		}

		var cohortId = await _adminRepository.InsertCohortAsync(dto, createdBy, ipAddress, cancellationToken);

		var allCohorts = await _adminRepository.GetAllCohortsAsync(cancellationToken);
		var created = allCohorts.FirstOrDefault(c => c.CohortId == cohortId);

		if (created is null)
		{
			return ApiResponse<CohortResponseDto>.FailureResult(new[] { "Cohort created but could not be retrieved." }, "Cohort creation failed.");
		}

		return ApiResponse<CohortResponseDto>.SuccessResult(created, "Cohort created successfully.");
	}

	public async Task<ApiResponse<List<CohortResponseDto>>> GetAllCohortsAsync(CancellationToken cancellationToken = default)
	{
		var result = await _adminRepository.GetAllCohortsAsync(cancellationToken);
		return ApiResponse<List<CohortResponseDto>>.SuccessResult(result, "Cohorts retrieved successfully.");
	}

	public async Task<ApiResponse<bool>> AssignUserToCohortAsync(AssignUserToCohortRequestDto dto, string updatedBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		if (dto.UserId <= 0)
		{
			return ApiResponse<bool>.FailureResult(new[] { "UserId must be greater than zero." }, "Validation failed.");
		}

		await _adminRepository.AssignUserToCohortAsync(dto, updatedBy, ipAddress, cancellationToken);
		return ApiResponse<bool>.SuccessResult(true, "User assigned to cohort successfully.");
	}

	public async Task<ApiResponse<List<CohortMemberDto>>> GetCohortMembersAsync(long cohortId, CancellationToken cancellationToken = default)
	{
		if (cohortId <= 0)
		{
			return ApiResponse<List<CohortMemberDto>>.FailureResult(new[] { "CohortId must be greater than zero." }, "Validation failed.");
		}

		try
		{
			var result = await _adminRepository.GetCohortMembersAsync(cohortId, cancellationToken);
			return ApiResponse<List<CohortMemberDto>>.SuccessResult(result, "Cohort members retrieved successfully.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "GetCohortMembersAsync failed for CohortId={CohortId}", cohortId);
			return ApiResponse<List<CohortMemberDto>>.FailureResult(new[] { "Failed to retrieve cohort members." }, "An error occurred.");
		}
	}

	public async Task<ApiResponse<CohortAnalyticsResponseDto>> GetCohortAnalyticsAsync(long cohortId, CancellationToken cancellationToken = default)
	{
		if (cohortId <= 0)
		{
			return ApiResponse<CohortAnalyticsResponseDto>.FailureResult(new[] { "CohortId must be greater than zero." }, "Validation failed.");
		}

		var result = await _adminRepository.GetCohortAnalyticsAsync(cohortId, cancellationToken);

		if (result is null)
		{
			return ApiResponse<CohortAnalyticsResponseDto>.FailureResult(new[] { "Cohort not found." }, "Cohort analytics failed.");
		}

		return ApiResponse<CohortAnalyticsResponseDto>.SuccessResult(result, "Cohort analytics retrieved successfully.");
	}

	private static bool IsValidHintLanguage(string value) =>
		value is "Telugu" or "Hindi" or "Tamil" or "Kannada" or "None";

	private static void NormalizeUserSearch(AdminUserSearchRequestDto dto)
	{
		dto.SearchTerm = string.IsNullOrWhiteSpace(dto.SearchTerm) ? null : dto.SearchTerm.Trim();
		dto.AgeGroup = string.IsNullOrWhiteSpace(dto.AgeGroup) ? null : dto.AgeGroup.Trim();
		dto.PageNumber = dto.PageNumber <= 0 ? 1 : dto.PageNumber;
		dto.PageSize = dto.PageSize <= 0 ? 10 : dto.PageSize;
	}

	private static void NormalizeReportFilter(AdminReportFilterRequestDto dto)
	{
		dto.PageNumber = dto.PageNumber <= 0 ? 1 : dto.PageNumber;
		dto.PageSize = dto.PageSize <= 0 ? 10 : dto.PageSize;
		dto.UserId = dto.UserId is > 0 ? dto.UserId : null;
	}

	private static void NormalizeSessionHistoryFilter(AdminSessionHistoryFilterRequestDto dto)
	{
		dto.SearchTerm = string.IsNullOrWhiteSpace(dto.SearchTerm) ? null : dto.SearchTerm.Trim();
		dto.Status     = string.IsNullOrWhiteSpace(dto.Status)     ? null : dto.Status.Trim().ToUpperInvariant();
		dto.PageNumber = dto.PageNumber <= 0 ? 1  : dto.PageNumber;
		dto.PageSize   = dto.PageSize   <= 0 ? 20 : dto.PageSize;
	}

}
