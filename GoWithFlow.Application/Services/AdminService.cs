using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.Admin;
using GoWithFlow.Application.DTOs.Responses.Admin;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;

namespace GoWithFlow.Application.Services;

public sealed class AdminService : IAdminService
{
	private readonly IAdminRepository _adminRepository;
	private readonly IUserRepository _userRepository;
	private readonly IMemoryCache _memoryCache;
	private readonly IExcelExportService _excelExportService;

	public AdminService(
		IAdminRepository adminRepository,
		IUserRepository userRepository,
		IMemoryCache memoryCache,
		IExcelExportService excelExportService)
	{
		_adminRepository = adminRepository;
		_userRepository = userRepository;
		_memoryCache = memoryCache;
		_excelExportService = excelExportService;
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

	public async Task<ApiResponse<byte[]>> ExportReportsAsExcelAsync(AdminReportFilterRequestDto dto, CancellationToken cancellationToken = default)
	{
		NormalizeReportFilter(dto);

		var exportFilter = new AdminReportFilterRequestDto
		{
			FromDate = dto.FromDate,
			ToDate = dto.ToDate,
			UserId = dto.UserId,
			PageNumber = 1,
			PageSize = 10000
		};

		var workbookBytes = await _excelExportService.GenerateUserReportExcelAsync(exportFilter);

		return ApiResponse<byte[]>.SuccessResult(workbookBytes, "Admin reports exported successfully.");
	}

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

}
