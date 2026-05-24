using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.Admin;
using GoWithFlow.Application.DTOs.Responses.Admin;

namespace GoWithFlow.Application.Interfaces.Repositories;

public interface IAdminRepository
{
	Task<AdminDashboardResponseDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);

	Task<List<RecentActivityDto>> GetRecentActivitiesAsync(int topN, CancellationToken cancellationToken = default);

	Task<List<GrammarMistakeSummaryDto>> GetTopGrammarMistakesAsync(int topN, CancellationToken cancellationToken = default);

	Task<PagedResult<AdminUserListResponseDto>> GetUsersAsync(AdminUserSearchRequestDto dto, CancellationToken cancellationToken = default);

	Task<AdminUserDetailResponseDto?> GetUserDetailAsync(long userId, CancellationToken cancellationToken = default);

	Task UpdateUserStatusAsync(UpdateUserStatusRequestDto dto, string updatedBy, string ipAddress, CancellationToken cancellationToken = default);

	Task<AdminNoteResponseDto> AddAdminNoteAsync(long adminUserId, AdminNoteRequestDto dto, string createdBy, string ipAddress, CancellationToken cancellationToken = default);

	Task<List<AdminNoteResponseDto>> GetAdminNotesByUserAsync(long targetUserId, CancellationToken cancellationToken = default);

	Task<PagedResult<AdminReportSummaryDto>> GetReportSummaryAsync(AdminReportFilterRequestDto dto, CancellationToken cancellationToken = default);

	Task<AdminUserFullReportDto?> GetUserFullReportAsync(long userId, CancellationToken cancellationToken = default);

	Task<PagedResult<AdminSessionHistoryItemDto>> GetSessionHistoryAsync(AdminSessionHistoryFilterRequestDto dto, CancellationToken cancellationToken = default);
}
