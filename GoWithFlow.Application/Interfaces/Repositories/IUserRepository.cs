using GoWithFlow.Domain.Entities;
using GoWithFlow.Application.DTOs.Responses;
using GoWithFlow.Application.DTOs.Responses.User;

namespace GoWithFlow.Application.Interfaces.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
	Task<User?> GetByMobileNumberAsync(string mobileNumber, CancellationToken cancellationToken = default);

	Task<User?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);

	Task<long> InsertUserAsync(User user, CancellationToken cancellationToken = default);

	Task UpdateLastLoginAsync(long userId, string updatedBy, string ipAddress, CancellationToken cancellationToken = default);

	Task SoftDeleteUserAsync(long userId, string deletedBy, string ipAddress, CancellationToken cancellationToken = default);

	Task<UserProfileResponseDto?> GetUserProfileAsync(long userId, CancellationToken cancellationToken = default);

	Task<UserDashboardResponseDto?> GetUserDashboardAsync(long userId, CancellationToken cancellationToken = default);

	Task UpdateUserProfileAsync(
		long userId,
		string fullName,
		string? email,
		string ageGroup,
		string preferredHintLanguage,
		string? avatarUrl,
		string updatedBy,
		string ipAddress,
		CancellationToken cancellationToken = default);

	Task UpsertUserStreakAsync(long userId, int practiceMinutes, string updatedBy, string ipAddress, CancellationToken cancellationToken = default);

	Task<StreakDataResponseDto> GetStreakDataAsync(long userId, CancellationToken cancellationToken = default);

	Task<List<UserBadgeDto>> GetBadgesAsync(long userId, CancellationToken cancellationToken = default);

	Task CheckAndAwardBadgesAsync(long userId, string createdBy, string ipAddress, CancellationToken cancellationToken = default);

	Task<SessionDetailResponseDto?> GetSessionDetailAsync(long sessionId, long userId, CancellationToken cancellationToken = default);

	Task<List<SessionScoreDto>> GetImprovementSessionsAsync(long userId, CancellationToken cancellationToken = default);

	Task<List<WeeklyScoreDto>> GetWeeklyFluencyScoresAsync(long userId, CancellationToken cancellationToken = default);

	Task<List<GrammarProgressResponseDto>> GetGrammarProgressAsync(long userId, CancellationToken cancellationToken = default);

	Task<List<RepracticeSessionResponseDto>> GetRepracticeHistoryAsync(long userId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
}
