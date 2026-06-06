using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.Admin;
using GoWithFlow.Application.DTOs.Responses.Admin;

namespace GoWithFlow.Application.Interfaces.Services;

public interface IAdminService
{
	Task<ApiResponse<AdminDashboardResponseDto>> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);

	Task<ApiResponse<PagedResult<AdminUserListResponseDto>>> GetUsersAsync(AdminUserSearchRequestDto dto, CancellationToken cancellationToken = default);

	Task<ApiResponse<AdminUserDetailResponseDto>> GetUserDetailAsync(long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<bool>> UpdateUserStatusAsync(UpdateUserStatusRequestDto dto, CancellationToken cancellationToken = default);

	Task<ApiResponse<AdminNoteResponseDto>> AddAdminNoteAsync(AdminNoteRequestDto dto, long adminUserId, CancellationToken cancellationToken = default);

	Task<ApiResponse<List<AdminNoteResponseDto>>> GetAdminNotesByUserAsync(long targetUserId, CancellationToken cancellationToken = default);

	Task<ApiResponse<PagedResult<AdminReportSummaryDto>>> GetReportSummaryAsync(AdminReportFilterRequestDto dto, CancellationToken cancellationToken = default);

	Task<ApiResponse<AdminUserFullReportDto>> GetUserFullReportAsync(long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<string>> ExportReportsAsExcelAsync(AdminReportFilterRequestDto dto, CancellationToken cancellationToken = default);

	Task<ApiResponse<AdminCreateUserResponseDto>> CreateUserAsync(AdminCreateUserRequestDto dto, CancellationToken cancellationToken = default);

	Task<ApiResponse<string?>> UpdateUserAsync(long userId, AdminUpdateUserRequestDto dto, CancellationToken cancellationToken = default);

	Task<ApiResponse<PagedResult<AdminSessionHistoryItemDto>>> GetSessionHistoryAsync(AdminSessionHistoryFilterRequestDto dto, CancellationToken cancellationToken = default);

	Task<ApiResponse<AdminSessionHistoryItemDto>> GetSessionByIdAsync(long sessionId, CancellationToken cancellationToken = default);

	Task<ApiResponse<CohortResponseDto>> CreateCohortAsync(CreateCohortRequestDto dto, string createdBy, string ipAddress, CancellationToken cancellationToken = default);

	Task<ApiResponse<List<CohortResponseDto>>> GetAllCohortsAsync(CancellationToken cancellationToken = default);

	Task<ApiResponse<bool>> AssignUserToCohortAsync(AssignUserToCohortRequestDto dto, string updatedBy, string ipAddress, CancellationToken cancellationToken = default);

	Task<ApiResponse<List<CohortMemberDto>>> GetCohortMembersAsync(long cohortId, CancellationToken cancellationToken = default);

	Task<ApiResponse<CohortAnalyticsResponseDto>> GetCohortAnalyticsAsync(long cohortId, CancellationToken cancellationToken = default);
}
