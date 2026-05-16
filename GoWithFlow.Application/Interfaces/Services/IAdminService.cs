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

	Task<ApiResponse<byte[]>> ExportReportsAsExcelAsync(AdminReportFilterRequestDto dto, CancellationToken cancellationToken = default);
}
