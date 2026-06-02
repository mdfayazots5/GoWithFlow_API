using System.Security.Cryptography;
using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.Admin;
using GoWithFlow.Application.DTOs.Responses.Admin;
using GoWithFlow.Application.Helpers;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Application.Interfaces.Services;
using GoWithFlow.Application.Settings;
using GoWithFlow.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
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

	public AdminService(
		IAdminRepository adminRepository,
		IUserRepository userRepository,
		IMemoryCache memoryCache,
		IExcelExportService excelExportService,
		IStorageService storageService,
		IOptions<CloudflareR2Settings> r2Options)
	{
		_adminRepository    = adminRepository;
		_userRepository     = userRepository;
		_memoryCache        = memoryCache;
		_excelExportService = excelExportService;
		_storageService     = storageService;
		_r2Settings         = r2Options.Value;
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
		_memoryCache.Set(CacheKeys.AdminDashboardStats, dashboardSummary, TimeSpan.FromMinutes(2));

		return ApiResponse<AdminDashboardResponseDto>.SuccessResult(dashboardSummary, "Admin dashboard summary retrieved successfully.");
	}

	public async Task<ApiResponse<PagedResult<AdminUserListResponseDto>>> GetUsersAsync(AdminUserSearchRequestDto dto, CancellationToken cancellationToken = default)
	{
		NormalizeUserSearch(dto);

		var users = await _adminRepository.GetUsersAsync(dto, cancellationToken);

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

		var response = new AdminCreateUserResponseDto
		{
			UserId = userId,
			FullName = user.FullName,
			MobileNumber = user.MobileNumber,
			AgeGroup = user.AgeGroup,
			Status = "ACTIVE"
		};

		return ApiResponse<AdminCreateUserResponseDto>.SuccessResult(response, "User created successfully.");
	}

	public async Task<ApiResponse<bool>> UpdateUserAsync(long userId, AdminUpdateUserRequestDto dto, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(dto.FullName))
			return ApiResponse<bool>.FailureResult(new[] { "Full name is required." }, "Validation failed.");

		if (string.IsNullOrWhiteSpace(dto.MobileNumber))
			return ApiResponse<bool>.FailureResult(new[] { "Mobile number is required." }, "Validation failed.");

		if (string.IsNullOrWhiteSpace(dto.AgeGroup) || !IsValidAgeGroup(dto.AgeGroup))
			return ApiResponse<bool>.FailureResult(new[] { "Age group must be one of: Child (6-12), Teen (13-17), Adult (18+)." }, "Validation failed.");

		if (string.IsNullOrWhiteSpace(dto.PreferredHintLanguage) || !IsValidHintLanguage(dto.PreferredHintLanguage))
			return ApiResponse<bool>.FailureResult(new[] { "Preferred language must be one of: Telugu, Hindi, Tamil, Kannada, None." }, "Validation failed.");

		var user = await _userRepository.GetByUserIdAsync(userId, cancellationToken);
		if (user is null)
			return ApiResponse<bool>.FailureResult(new[] { "User not found." }, "Update failed.");

		if (!string.Equals(user.MobileNumber, dto.MobileNumber.Trim(), StringComparison.OrdinalIgnoreCase))
		{
			var existing = await _userRepository.GetByMobileNumberAsync(dto.MobileNumber.Trim(), cancellationToken);
			if (existing is not null)
				return ApiResponse<bool>.FailureResult(new[] { "Mobile number is already registered." }, "Update failed.");
		}

		user.FullName = dto.FullName.Trim();
		user.MobileNumber = dto.MobileNumber.Trim();
		user.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
		user.AgeGroup = dto.AgeGroup.Trim();
		user.PreferredHintLanguage = dto.PreferredHintLanguage.Trim();
		user.UpdatedBy = "Admin";
		user.LastUpdated = DateTime.UtcNow;

		if (!string.IsNullOrWhiteSpace(dto.Password))
			user.PasswordHash = HashPassword(dto.Password);

		_userRepository.Update(user);
		await _userRepository.SaveChangesAsync(cancellationToken);

		return ApiResponse<bool>.SuccessResult(true, "User updated successfully.");
	}

	private static string HashPassword(string password)
	{
		byte[] salt = RandomNumberGenerator.GetBytes(16);
		byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
		return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
	}

	private static bool IsValidAgeGroup(string value) =>
		value is "Child (6-12)" or "Teen (13-17)" or "Adult (18+)";

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

		var result = await _adminRepository.GetCohortMembersAsync(cohortId, cancellationToken);
		return ApiResponse<List<CohortMemberDto>>.SuccessResult(result, "Cohort members retrieved successfully.");
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
